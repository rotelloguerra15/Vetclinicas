using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

/// <summary>
/// Maquina de estados do chatbot de agendamento via WhatsApp.
/// Estados: inicio -> aguardando_servico -> aguardando_data -> aguardando_horario -> concluido
/// </summary>
public class BotWhatsAppService
{
    private readonly AppDbContext _db;
    private readonly AgendaService _agenda;
    private readonly ZApiService _zapi;
    private readonly ILogger<BotWhatsAppService> _log;

    public BotWhatsAppService(AppDbContext db, AgendaService agenda, ZApiService zapi,
        ILogger<BotWhatsAppService> log)
    { _db = db; _agenda = agenda; _zapi = zapi; _log = log; }

    public async Task ProcessarMensagem(Guid tenantId, string telefone, string texto)
    {
        var cfg = await _db.BotConfigs.FirstOrDefaultAsync(b => b.TenantId == tenantId);
        if (cfg == null || !cfg.Ativo) return;

        // Verifica horario de funcionamento do bot
        var agora = TimeOnly.FromDateTime(DateTime.Now);
        if (agora < cfg.HoraInicio || agora > cfg.HoraFim)
        {
            await Responder(tenantId, telefone, cfg.MsgForaHorario
                .Replace("{inicio}", cfg.HoraInicio.ToString("HH:mm"))
                .Replace("{fim}", cfg.HoraFim.ToString("HH:mm")), cfg, null, "fora_horario");
            return;
        }

        // Carrega ou cria conversa
        var conv = await _db.BotConversas
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Telefone == telefone);

        var estadoAntes = conv?.Estado ?? "novo";

        // Timeout: reinicia se inativo ha muito tempo
        if (conv != null && conv.UltimaMsgEm < DateTime.UtcNow.AddMinutes(-cfg.TimeoutConversaMin))
        {
            _db.BotConversas.Remove(conv);
            await _db.SaveChangesAsync();
            conv = null;
        }

        if (conv == null)
        {
            conv = new BotConversa
            {
                Id        = Guid.NewGuid(),
                TenantId  = tenantId,
                Telefone  = telefone,
                Estado    = "inicio",
                UltimaMsgEm = DateTime.UtcNow,
                CriadoEm  = DateTime.UtcNow
            };
            _db.BotConversas.Add(conv);
        }
        else
        {
            conv.UltimaMsgEm = DateTime.UtcNow;
        }

        var textoLower = texto.Trim().ToLower();

        // Comandos globais
        if (textoLower is "menu" or "oi" or "ola" or "olá" or "inicio" or "inicio" or "1")
        {
            conv.Estado = "inicio";
            conv.PetId = null; conv.PetNomeDigitado = null;
            conv.ServicoId = null; conv.ServicoNome = null;
            conv.DataEscolhida = null;
        }

        if (textoLower is "cancelar" or "0" or "sair")
        {
            conv.Estado = "cancelado";
            await _db.SaveChangesAsync();
            await Responder(tenantId, telefone, cfg.MsgCancelar, cfg, estadoAntes, "cancelado");
            return;
        }

        await ProcessarEstado(conv, textoLower, cfg, tenantId, telefone, estadoAntes);
        await _db.SaveChangesAsync();
    }

    private async Task ProcessarEstado(BotConversa conv, string texto, BotConfig cfg,
        Guid tenantId, string telefone, string estadoAntes)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        var clinicaNome = tenant?.Nome ?? "Clinica";

        switch (conv.Estado)
        {
            case "inicio":
                await IniciarConversa(conv, cfg, tenantId, telefone, clinicaNome, estadoAntes);
                break;

            case "aguardando_servico":
                await ProcessarEscolhaServico(conv, texto, cfg, tenantId, telefone, estadoAntes);
                break;

            case "aguardando_data":
                await ProcessarEscolhaData(conv, texto, cfg, tenantId, telefone, estadoAntes);
                break;

            case "aguardando_horario":
                await ProcessarEscolhaHorario(conv, texto, cfg, tenantId, telefone, estadoAntes);
                break;

            default:
                conv.Estado = "inicio";
                await IniciarConversa(conv, cfg, tenantId, telefone, clinicaNome, estadoAntes);
                break;
        }
    }

    // ── ESTADO: inicio ────────────────────────────────────────────────────────

    private async Task IniciarConversa(BotConversa conv, BotConfig cfg,
        Guid tenantId, string telefone, string clinicaNome, string estadoAntes)
    {
        // Tenta identificar o tutor pelo telefone
        var tutor = await _db.Tutores
            .FirstOrDefaultAsync(t => t.TenantId == tenantId
                && (t.Telefone != null && t.Telefone.Contains(telefone.TakeLast(9).ToArray().ToString() ?? telefone)));

        // Busca servicos ativos
        var servicos = await _db.Servicos
            .Where(s => s.TenantId == tenantId && s.Ativo)
            .OrderBy(s => s.Nome)
            .ToListAsync();

        if (!servicos.Any())
        {
            await Responder(tenantId, telefone,
                "Desculpe, nao ha servicos cadastrados no momento. Entre em contato diretamente.",
                cfg, estadoAntes, "sem_servicos");
            return;
        }

        conv.TutorId = tutor?.Id;
        conv.Estado  = "aguardando_servico";

        var bv = cfg.MsgBoasVindas.Replace("{clinica}", clinicaNome);
        if (tutor != null) bv = bv.Replace("{tutor}", tutor.Nome);

        var lista = string.Join("\n", servicos.Select((s, i) =>
            $"{i + 1} - {s.Nome}{(s.PrecoBase > 0 ? $" (R$ {s.PrecoBase:F2})" : "")}"));

        var msg = $"{bv}\n\n{cfg.MsgQualServico}\n\n{lista}\n\n0 - Cancelar";

        await Responder(tenantId, telefone, msg, cfg, estadoAntes, "aguardando_servico");
    }

    // ── ESTADO: aguardando_servico ────────────────────────────────────────────

    private async Task ProcessarEscolhaServico(BotConversa conv, string texto, BotConfig cfg,
        Guid tenantId, string telefone, string estadoAntes)
    {
        var servicos = await _db.Servicos
            .Where(s => s.TenantId == tenantId && s.Ativo)
            .OrderBy(s => s.Nome).ToListAsync();

        Servico? escolhido = null;

        if (int.TryParse(texto, out var num) && num >= 1 && num <= servicos.Count)
            escolhido = servicos[num - 1];
        else
            escolhido = servicos.FirstOrDefault(s =>
                s.Nome.ToLower().Contains(texto));

        if (escolhido == null)
        {
            await Responder(tenantId, telefone, cfg.MsgErro, cfg, estadoAntes, conv.Estado);
            return;
        }

        conv.ServicoId   = escolhido.Id;
        conv.ServicoNome = escolhido.Nome;
        conv.DuracaoMin  = escolhido.DuracaoMin ?? 60;
        conv.Estado      = "aguardando_data";

        // Oferece as proximas datas com slots disponiveis
        var proximasDatas = await GetProximasDatasDisponiveis(tenantId, conv.DuracaoMin.Value, cfg);

        if (!proximasDatas.Any())
        {
            await Responder(tenantId, telefone,
                $"Servico *{escolhido.Nome}* selecionado!\n\n" +
                "Infelizmente nao ha horarios disponiveis nos proximos dias. " +
                "Entre em contato para verificar disponibilidade.",
                cfg, estadoAntes, conv.Estado);
            return;
        }

        var listaDatas = string.Join("\n", proximasDatas.Select((d, i) =>
            $"{i + 1} - {d:dddd, dd/MM}".Replace("Sunday","domingo").Replace("Monday","segunda")
             .Replace("Tuesday","terca").Replace("Wednesday","quarta").Replace("Thursday","quinta")
             .Replace("Friday","sexta").Replace("Saturday","sabado")));

        var msg = $"Servico: *{escolhido.Nome}*\n" +
            $"Duracao: {conv.DuracaoMin} min\n\n" +
            $"{cfg.MsgQualData}\n\n{listaDatas}\n\nOu digite a data desejada (ex: 25/06)\n\n0 - Cancelar";

        await Responder(tenantId, telefone, msg, cfg, estadoAntes, "aguardando_data");
    }

    // ── ESTADO: aguardando_data ───────────────────────────────────────────────

    private async Task ProcessarEscolhaData(BotConversa conv, string texto, BotConfig cfg,
        Guid tenantId, string telefone, string estadoAntes)
    {
        DateOnly? data = null;

        // Numero da lista
        var datas = await GetProximasDatasDisponiveis(tenantId, conv.DuracaoMin ?? 60, cfg);
        if (int.TryParse(texto, out var num) && num >= 1 && num <= datas.Count)
        {
            data = datas[num - 1];
        }
        // "amanha"
        else if (texto is "amanha" or "amanhã")
        {
            data = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        }
        // "hoje"
        else if (texto == "hoje")
        {
            data = DateOnly.FromDateTime(DateTime.Today);
        }
        // dd/MM ou dd/MM/yyyy
        else
        {
            var formatos = new[] { "dd/MM", "dd/MM/yyyy", "d/M", "d/M/yyyy" };
            foreach (var fmt in formatos)
            {
                var ano = fmt.Contains("yyyy") ? "" : $"/{DateTime.Today.Year}";
                if (DateOnly.TryParseExact(texto + ano, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d))
                {
                    data = d; break;
                }
            }
        }

        if (data == null || data < DateOnly.FromDateTime(DateTime.Today))
        {
            await Responder(tenantId, telefone,
                "Data invalida. Digite o numero da lista ou a data no formato dd/MM (ex: 25/06).",
                cfg, estadoAntes, conv.Estado);
            return;
        }

        // Busca slots para essa data
        var inicio = data.Value.ToDateTime(TimeOnly.MinValue);
        var slots  = await _agenda.GetSlotsLivres(tenantId, inicio, 1, conv.DuracaoMin ?? 60);
        slots = slots.Where(s => DateOnly.FromDateTime(s.Inicio) == data).ToList();

        if (!slots.Any())
        {
            await Responder(tenantId, telefone, cfg.MsgSemHorarios, cfg, estadoAntes, conv.Estado);
            return;
        }

        conv.DataEscolhida = data;
        conv.Estado        = "aguardando_horario";

        var dataFmt = data.Value.ToString("dd/MM");
        var listaHorarios = string.Join("\n", slots.Take(10).Select((s, i) =>
            $"{i + 1} - {s.Inicio:HH:mm}"));

        var msg = cfg.MsgHorariosDisponiveis.Replace("{data}", dataFmt) +
            $"\n\n{listaHorarios}\n\n0 - Outra data";

        await Responder(tenantId, telefone, msg, cfg, estadoAntes, "aguardando_horario");
    }

    // ── ESTADO: aguardando_horario ────────────────────────────────────────────

    private async Task ProcessarEscolhaHorario(BotConversa conv, string texto, BotConfig cfg,
        Guid tenantId, string telefone, string estadoAntes)
    {
        if (texto == "0")
        {
            conv.Estado       = "aguardando_data";
            conv.DataEscolhida = null;
            await Responder(tenantId, telefone,
                "Ok! Para qual data voce gostaria? (ex: amanha, 25/06)",
                cfg, estadoAntes, "aguardando_data");
            return;
        }

        var inicio = conv.DataEscolhida!.Value.ToDateTime(TimeOnly.MinValue);
        var slots  = await _agenda.GetSlotsLivres(tenantId, inicio, 1, conv.DuracaoMin ?? 60);
        slots      = slots.Where(s => DateOnly.FromDateTime(s.Inicio) == conv.DataEscolhida).ToList();

        if (!int.TryParse(texto, out var num) || num < 1 || num > slots.Count)
        {
            await Responder(tenantId, telefone, cfg.MsgErro, cfg, estadoAntes, conv.Estado);
            return;
        }
        var escolhido = slots[num - 1];

        // Criar agendamento
        var tutor = conv.TutorId.HasValue
            ? await _db.Tutores.FirstOrDefaultAsync(t => t.Id == conv.TutorId.Value)
            : null;

        // Tenta encontrar pet do tutor
        Guid? petId = null;
        if (tutor != null)
        {
            var pet = await _db.Pets
                .Where(p => p.TutorId == tutor.Id && p.TenantId == tenantId && p.Ativo)
                .OrderBy(p => p.Nome)
                .FirstOrDefaultAsync();
            petId = pet?.Id;
        }

        if (petId == null)
        {
            // Sem pet identificado — cria agendamento sem pet por enquanto
            // (a clinica vai ver na agenda e completar)
            await Responder(tenantId, telefone,
                "Nao consegui identificar seu pet. Entre em contato com a clinica para confirmar.",
                cfg, estadoAntes, conv.Estado);
            return;
        }

        var ag = new Agendamento
        {
            Id         = Guid.NewGuid(),
            TenantId   = tenantId,
            PetId      = petId.Value,
            Tipo       = conv.ServicoNome?.ToLower().Replace(" ", "_") ?? "servico",
            DataHora   = escolhido.Inicio,
            DuracaoMin = conv.DuracaoMin ?? 60,
            Status     = "pendente",
            Origem     = "bot_whatsapp",
            CriadoEm   = DateTime.UtcNow
        };
        _db.Agendamentos.Add(ag);

        conv.Estado = "concluido";

        // Notifica a clinica
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant?.WhatsappNumber != null)
        {
            var notif = $"Novo agendamento via bot!\n\n" +
                $"Servico: {conv.ServicoNome}\n" +
                $"Data: {escolhido.Inicio:dd/MM/yyyy} as {escolhido.Inicio:HH:mm}\n" +
                $"Telefone tutor: {telefone}\n\n" +
                $"Acesse o sistema para confirmar.";
            await _zapi.EnviarTexto(tenant.WhatsappNumber, notif);
        }

        var msg = cfg.MsgConfirmacao
            .Replace("{pet}", conv.PetNomeDigitado ?? "seu pet")
            .Replace("{servico}", conv.ServicoNome ?? "")
            .Replace("{data}", escolhido.Inicio.ToString("dd/MM/yyyy"))
            .Replace("{hora}", escolhido.Inicio.ToString("HH:mm"));

        await Responder(tenantId, telefone, msg, cfg, estadoAntes, "concluido");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<List<DateOnly>> GetProximasDatasDisponiveis(
        Guid tenantId, int duracaoMin, BotConfig cfg)
    {
        var inicio = DateTime.Today.AddDays(cfg.DiasAntecedenciaMin);
        var slots  = await _agenda.GetSlotsLivres(tenantId, inicio, cfg.DiasAntecedenciaMax, duracaoMin);
        return slots
            .Select(s => DateOnly.FromDateTime(s.Inicio))
            .Distinct()
            .Take(7)
            .ToList();
    }

    private async Task Responder(Guid tenantId, string telefone, string mensagem,
        BotConfig cfg, string? estadoAntes, string? estadoApos)
    {
        await _zapi.EnviarTexto(telefone, mensagem);

        _db.BotLogs.Add(new BotLog
        {
            Id         = Guid.NewGuid(),
            TenantId   = tenantId,
            Telefone   = telefone,
            Direcao    = "saida",
            Mensagem   = mensagem,
            EstadoAntes = estadoAntes,
            EstadoApos  = estadoApos,
            CriadoEm   = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
