using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;

namespace VetClinica.API.Services;

// Worker em background: processa a fila de notificações a cada 5 minutos.
// Dispara "pet pronto", "lembrete 30 dias" e "confirmação de agenda".
public class NotificacaoDispatcher : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<NotificacaoDispatcher> _log;
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

    public NotificacaoDispatcher(IServiceProvider sp, ILogger<NotificacaoDispatcher> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessarFila(ct); }
            catch (Exception ex) { _log.LogError(ex, "Erro no dispatcher de notificações"); }
            await Task.Delay(Intervalo, ct);
        }
    }

    private async Task ProcessarFila(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var zapi = scope.ServiceProvider.GetRequiredService<WhatsAppService>();
        var linkService = scope.ServiceProvider.GetRequiredService<AgendamentoLinkService>();

        var pendentes = await db.NotificacoesFila
            .Where(n => n.Status == "pendente" && n.AgendadoPara <= DateTime.UtcNow)
            .OrderBy(n => n.AgendadoPara)
            .Take(50)
            .ToListAsync(ct);

        foreach (var notif in pendentes)
        {
            bool ok = false;
            try
            {
                var payload = string.IsNullOrEmpty(notif.Payload)
                    ? new Dictionary<string, JsonElement>()
                    : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(notif.Payload)!;

                string petNome = payload.TryGetValue("pet_nome", out var pn) ? pn.ToString() : "seu pet";

                if (string.IsNullOrEmpty(notif.Destinatario))
                {
                    notif.Status = "falhou";
                    notif.Erro = "Sem destinatário";
                    continue;
                }

                switch (notif.Tipo)
                {
                    case "pet_pronto":
                        ok = await zapi.EnviarTexto(notif.Destinatario,
                            $"🐾 Oi! O *{petNome}* já está prontinho e te esperando aqui na clínica! 🛁✨");
                        if (ok && payload.TryGetValue("fotos", out var fotos) && fotos.ValueKind == JsonValueKind.Array)
                            foreach (var f in fotos.EnumerateArray())
                                await zapi.EnviarImagem(notif.Destinatario, f.ToString(), petNome);
                        break;

                    case "lembrete_banho":
                        var link = await linkService.GerarLinkAgendamento(
                            notif.TenantId, notif.TutorId!.Value, notif.PetId);
                        ok = await zapi.EnviarTexto(notif.Destinatario,
                            $"🐕 Olá! Já faz 30 dias que o *{petNome}* não passa por aqui.\n\n" +
                            $"Que tal agendar um banho? Clique no link e escolha o melhor dia:\n👉 {link}");
                        break;

                    case "confirmacao_agenda":
                        ok = await zapi.EnviarTexto(notif.Destinatario,
                            $"✅ Agendamento do *{petNome}* confirmado! Te esperamos. 🐾");
                        break;

                    case "promocao":
                        // mensagem da campanha + imagem opcional
                        var textoPromo = payload.TryGetValue("mensagem", out var mp) ? mp.ToString() : "";
                        ok = await zapi.EnviarTexto(notif.Destinatario, textoPromo);
                        if (ok && payload.TryGetValue("fotos", out var fp) && fp.ValueKind == JsonValueKind.Array)
                            foreach (var img in fp.EnumerateArray())
                                await zapi.EnviarImagem(notif.Destinatario, img.ToString());
                        break;

                    default:
                        ok = await zapi.EnviarTexto(notif.Destinatario,
                            payload.TryGetValue("mensagem", out var m) ? m.ToString() : "Mensagem da clínica.");
                        break;
                }

                notif.Status = ok ? "enviado" : "falhou";
                notif.EnviadoEm = ok ? DateTime.UtcNow : null;
                if (!ok) notif.Erro = "Falha no envio Z-API";
            }
            catch (Exception ex)
            {
                notif.Status = "falhou";
                notif.Erro = ex.Message;
                _log.LogError(ex, "Falha ao processar notificação {Id}", notif.Id);
            }
            finally
            {
                notif.Tentativas++;
            }
        }

        if (pendentes.Any())
        {
            await db.SaveChangesAsync(ct);
            _log.LogInformation("Processadas {N} notificações", pendentes.Count);
        }
    }
}
