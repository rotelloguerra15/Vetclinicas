using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

// Worker que roda 1x/hora e ENFILEIRA mensagens baseadas em gatilhos:
//  - aniversário do pet / tutor (no dia)
//  - lembrete de agendamento (X horas antes)
// O envio em si fica a cargo do NotificacaoDispatcher.
public class AgendadorMensagens : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<AgendadorMensagens> _log;
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    public AgendadorMensagens(IServiceProvider sp, ILogger<AgendadorMensagens> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // pequeno atraso inicial para não competir com a subida da app
        await Task.Delay(TimeSpan.FromSeconds(20), ct);
        while (!ct.IsCancellationRequested)
        {
            try { await Rodar(ct); }
            catch (Exception ex) { _log.LogError(ex, "Erro no agendador de mensagens"); }
            await Task.Delay(Intervalo, ct);
        }
    }

    private async Task Rodar(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>();
        var db = factory.Create();

        await ProcessarAniversarios(db, ct);
        await ProcessarLembretesAgendamento(db, ct);
    }

    private static string Render(string template, string clinica, string tutor, string pet,
        string data = "", string hora = "") =>
        template.Replace("{clinica}", clinica)
                .Replace("{tutor}", tutor)
                .Replace("{pet}", pet)
                .Replace("{data}", data)
                .Replace("{hora}", hora);

    // ---- Aniversários de pet e tutor (dispara uma vez por dia) ----
    private async Task ProcessarAniversarios(TenantDbContext db, CancellationToken ct)
    {
        var hoje = DateTime.Today;

        var configs = await db.ConfigMensagens
            .Where(c => c.Ativo && (c.Gatilho == "aniversario_pet" || c.Gatilho == "aniversario_tutor"))
            .ToListAsync(ct);

        foreach (var cfg in configs)
        {
            var clinica = await db.Tenants.Where(t => t.Id == cfg.TenantId)
                .Select(t => t.Nome).FirstOrDefaultAsync(ct) ?? "nossa clínica";

            if (cfg.Gatilho == "aniversario_pet")
            {
                // pets que fazem aniversário hoje (mês+dia)
                var pets = await db.Pets
                    .Where(p => p.TenantId == cfg.TenantId && p.Ativo && p.DataNascimento != null)
                    .Select(p => new { p.Id, p.Nome, p.TutorId, p.DataNascimento })
                    .ToListAsync(ct);

                foreach (var p in pets.Where(p =>
                    p.DataNascimento!.Value.Month == hoje.Month &&
                    p.DataNascimento.Value.Day == hoje.Day))
                {
                    var tutor = await db.Tutores.FirstOrDefaultAsync(t => t.Id == p.TutorId, ct);
                    if (tutor?.Telefone == null) continue;
                    if (await JaEnfileirado(db, cfg.TenantId, "aniversario_pet", p.Id, hoje, ct)) continue;

                    await Enfileirar(db, cfg, tutor.Id, tutor.Telefone, p.Id,
                        Render(cfg.Template, clinica, tutor.Nome, p.Nome), "aniversario_pet");
                }
            }
            else // aniversario_tutor (somente dia/mês — sem ano)
            {
                var tutores = await db.Tutores
                    .Where(t => t.TenantId == cfg.TenantId && t.Ativo && t.Telefone != null
                             && t.AniversarioDia == hoje.Day && t.AniversarioMes == hoje.Month)
                    .ToListAsync(ct);

                foreach (var t in tutores)
                {
                    // chave anti-duplicação por tutor (usa PetId nulo → diferenciamos pelo tutor)
                    bool jaFeito = await db.NotificacoesFila.AnyAsync(n =>
                        n.TenantId == cfg.TenantId && n.Tipo == "aniversario_tutor"
                        && n.TutorId == t.Id && n.CriadoEm >= hoje.AddDays(-1), ct);
                    if (jaFeito) continue;

                    // pega o nome de um pet do tutor para personalizar (se houver)
                    var petNome = await db.Pets
                        .Where(p => p.TutorId == t.Id && p.Ativo)
                        .Select(p => p.Nome).FirstOrDefaultAsync(ct) ?? "seu pet";

                    db.NotificacoesFila.Add(new NotificacaoFila
                    {
                        Id = Guid.NewGuid(),
                        TenantId = cfg.TenantId,
                        TutorId = t.Id,
                        Canal = cfg.Canal,
                        Tipo = "aniversario_tutor",
                        Status = "pendente",
                        Destinatario = t.Telefone,
                        Payload = JsonSerializer.Serialize(new
                        {
                            mensagem = Render(cfg.Template, clinica, t.Nome, petNome)
                        }),
                        AgendadoPara = DateTime.UtcNow,
                        CriadoEm = DateTime.UtcNow
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // ---- Lembrete de agendamento X horas antes ----
    private async Task ProcessarLembretesAgendamento(TenantDbContext db, CancellationToken ct)
    {
        var cfg = await db.ConfigMensagens
            .FirstOrDefaultAsync(c => c.Ativo && c.Gatilho == "lembrete_agendamento", ct);
        if (cfg == null) return;

        var horas = cfg.HorasAntes ?? 24;
        var agora = DateTime.UtcNow;
        var alvo = agora.AddHours(horas);

        // agendamentos confirmados dentro da janela [agora, alvo] ainda não lembrados
        var ags = await db.Agendamentos
            .Where(a => a.TenantId == cfg.TenantId
                     && a.Status == "confirmado"
                     && a.DataHora > agora && a.DataHora <= alvo)
            .ToListAsync(ct);

        var clinica = await db.Tenants.Where(t => t.Id == cfg.TenantId)
            .Select(t => t.Nome).FirstOrDefaultAsync(ct) ?? "nossa clínica";

        foreach (var ag in ags)
        {
            if (await JaEnfileirado(db, cfg.TenantId, "lembrete_agendamento", ag.PetId, ag.DataHora.Date, ct))
                continue;

            var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == ag.PetId, ct);
            if (pet == null) continue;
            var tutor = await db.Tutores.FirstOrDefaultAsync(t => t.Id == pet.TutorId, ct);
            if (tutor?.Telefone == null) continue;

            var local = ag.DataHora.ToLocalTime();
            var msg = Render(cfg.Template, clinica, tutor.Nome, pet.Nome,
                local.ToString("dd/MM"), local.ToString("HH:mm"));

            await Enfileirar(db, cfg, tutor.Id, tutor.Telefone, pet.Id, msg, "lembrete_agendamento");
        }
        await db.SaveChangesAsync(ct);
    }

    // Evita duplicar: já existe notif desse tipo, pra esse pet, criada hoje?
    private static async Task<bool> JaEnfileirado(TenantDbContext db, Guid tenantId, string tipo,
        Guid petId, DateTime referencia, CancellationToken ct)
    {
        var inicioDia = referencia.Date;
        return await db.NotificacoesFila.AnyAsync(n =>
            n.TenantId == tenantId && n.Tipo == tipo && n.PetId == petId
            && n.CriadoEm >= inicioDia.AddDays(-1), ct);
    }

    private static Task Enfileirar(TenantDbContext db, ConfigMensagem cfg, Guid tutorId,
        string telefone, Guid petId, string mensagem, string tipo)
    {
        db.NotificacoesFila.Add(new NotificacaoFila
        {
            Id = Guid.NewGuid(),
            TenantId = cfg.TenantId,
            PetId = petId,
            TutorId = tutorId,
            Canal = cfg.Canal,
            Tipo = tipo,
            Status = "pendente",
            Destinatario = telefone,
            Payload = JsonSerializer.Serialize(new { mensagem }),
            AgendadoPara = DateTime.UtcNow,
            CriadoEm = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }
}
