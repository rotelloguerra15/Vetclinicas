using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Models;
using VetClinica.API.Services;

namespace VetClinica.API.Services;

/// <summary>
/// Mquina de estados do chatbot de agendamento via WhatsApp.
/// Estados: inicio  aguardando_servico  aguardando_data  aguardando_horario  concluido
/// Migrado de ZApiService para WhatsAppService (Meta Cloud API).
/// </summary>
public class BotWhatsAppService
{
    private readonly AppDbContext _db;
    private readonly AgendaService _agenda;
    private readonly WhatsAppService _wa;   //  Meta Cloud API (era ZApiService)
    private readonly ILogger<BotWhatsAppService> _log;

    public BotWhatsAppService(AppDbContext db, AgendaService agenda,
        WhatsAppService wa, ILogger<BotWhatsAppService> log)
    { _db = db; _agenda = agenda; _wa = wa; _log = log; }

    public async Task ProcessarMensagem(Guid tenantId, string telefone, string texto)
    {
        var cfg = await _db.BotConfigs.FirstOrDefaultAsync(b => b.TenantId == tenantId);
        if (cfg == null || !cfg.Ativo) return;

        // Verifica horrio de funcionamento do bot
        var agora = TimeOnly.FromDateTime(DateTime.Now);
        if (agora < cfg.HoraInicio || agora > cfg.HoraFim)
        {
            await Responder(tenantId, telefone, cfg.MsgForaHorario
                .Replace("{inicio}", cfg.HoraInicio.ToString("HH:mm"))
                .Replace("{fim}",    cfg.HoraFim.ToString("HH:mm")),
                cfg, null, "fora_horario");
            return;
        }

        // Carrega ou cria conversa
        var conv = await _db.BotConversas
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Telefone == telefone);

        var estadoAntes = conv?.Estado ?? "novo";

        // Timeout: reinicia se inativo h muito tempo
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
                Id          = Guid.NewGuid(),
                TenantId    = tenantId,
                Telefone    = telefone,
                Estado      = "inicio",
                UltimaMsgEm = DateTime.UtcNow,
                CriadoEm    = DateTime.UtcNow
            };
            _db.BotConversas.Add(conv);
        }
        else
        {
            conv.UltimaMsgEm = DateTime.UtcNow;
        }

        var textoLower = texto.Trim().ToLower();

        // Comandos globais
        if (textoLower is "menu" or "oi" or "ola" or "ol" or "inicio" or "1")
        {
            conv.Estado          = "inicio";
            conv.PetId           = null;
            conv.PetNomeDigitado = null;
            conv.ServicoId       = null;
            conv.ServicoNome     = null;
            conv.DataEscolhida   = null;
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

    //  Roteador de estados 

    private async Task ProcessarEstado(BotConversa conv, string texto, BotConfig cfg,
        Guid tenantId, string telefone, string estadoAntes)
    {
        var tenant      = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
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
                await ProcessarEscolhaHorario(conv, texto, cfg, tenantId, telefone, estadoAntes, clinicaNome);
                break;
            default:
                conv.Estado = "inicio";
                await IniciarConversa(conv, cfg, tenantId, telefone, clinicaNome, estadoAntes);
                break;
        }
    }

    //  ESTADO: inicio 

    private async Task IniciarConversa(BotConversa conv, BotConfig cfg,
        Guid tenantId, string telefone, string clinicaNome, string estadoAntes)
    {
        // Identifica tutor pelo telefone (compara os ltimos 9 dgitos)
        var sufixo = telefone.Length >= 9 ? telefone[^9..] : telefone;
        var tutor  = await _db.Tutores
            .FirstOrDefaultAsync(t => t.TenantId == tenantId
                && t.Telefone != null && t.Telefone.EndsWith(sufixo));

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

        var saudacao = tutor != null ? $"Ola, *{tutor.Nome.Split(' ')[0]}*! " : "Ola! ";
        var bv       = cfg.MsgBoasVindas.Replace("{clinica}", clinicaNome);

        var lista = string.Join("\n", servicos.Select((s, i) =>
            $"{i + 1} - {s.Nome}{(s.PrecoBase > 0 ? $" (R$ {s.PrecoBase:F2})" : "")}"));

        var msg = $"{saudacao}\n{bv}\n\n{cfg.MsgQualServico}\n\n{lista}\n\n_0 - Cancelar_";
        await Responder(tenantId, telefone, msg, cfg, estadoAntes, "aguardando_servico");
    }

    //  ESTADO: aguardando_servico 

    private async Task ProcessarEscolhaServico(BotConversa conv, string texto, BotConfig cfg,
        Guid tenantId, string telefone, string estadoAntes)
    {
        var servicos = await _db.Servicos
            .Where(s => s.TenantId == tenantId && s.Ativo)
            .OrderBy(s => s.Nome)
            .ToListAsync();

        Servico? escolhido = null;
        if (int.TryParse(texto, out var num) && num >= 1 && num <= servicos.Count)
            escolhido = servicos[num - 1];
        else
            escolhido = servicos.FirstOrDefault(s => s.Nome.ToLower().Contains(texto));

        if (escolhido == null)
        {
            await Responder(tenantId, telefone, cfg.MsgErro, cfg, estadoAntes, conv.Estado);
            return;
        }

        conv.ServicoId   = escolhido.Id;
        conv.ServicoNome = escolhido.Nome;
        conv.DuracaoMin  = escolhido.DuracaoMin ?? 60;
        conv.Estado      = "aguardando_data";

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
        {
            var diasSemana = new[] { "domingo","segunda","terca","quarta","quinta","sexta","sabado" };
            return $"{i + 1} - {diasSemana[(int)d.DayOfWeek]}, {d:dd/MM}";
        }));

        var msg = $"Servico: *{escolhido.Nome}*\nDuracao: {conv.DuracaoMin} min\n\n" +
                  $"{cfg.MsgQualData}\n\n{listaDatas}\n\n" +
                  $"Ou digite a data (ex: 25/06)\n\n_0 - Cancelar_";

        await Responder(tenantId, telefone, msg, cfg, estadoAntes, "aguardando_data");
    }

    //  ESTADO: aguardando_data 

    private async Task ProcessarEscolhaData(BotConversa conv, string texto, BotConfig cfg,
        Guid tenantId, string telefone, string estadoAntes)
    {
        DateOnly? data = null;
        var datas      = await GetProximasDatasDisponiveis(tenantId, conv.DuracaoMin ?? 60, cfg);

        if (int.TryParse(texto, out var num) && num >= 1 && num <= datas.Count)
            data = datas[num - 1];
        else if (texto is "amanha" or "amanh")
            data = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        else if (texto == "hoje")
            data = DateOnly.FromDateTime(DateTime.Today);
        else
        {
            var ano = $"/{DateTime.Today.Year}";
            if (DateOnly.TryParseExact(texto + ano, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d))
                data = d;
        }

        if (data == null || data < DateOnly.FromDateTime(DateTime.Today))
        {
            await Responder(tenantId, telefone,
                "Data invalida. Digite o numero da lista ou a data (ex: 25/06).",
                cfg, estadoAntes, conv.Estado);
            return;
        }

        var inicio = data.Value.ToDateTime(TimeOnly.MinValue);
        var slots  = await _agenda.GetSlotsLivres(tenantId, inicio, 1, conv.DuracaoMin ?? 60);
        slots      = slots.Where(s => DateOnly.FromDateTime(s.Inicio) == data).ToList();

        if (!slots.Any())
        {
            await Responder(tenantId, telefone, cfg.MsgSemHorarios, cfg, estadoAntes, conv.Estado);
            return;
        }

        conv.DataEscolhida = data;
        conv.Estado        = "aguardando_horario";

        var listaHorarios = string.Join("\n", slots.Take(10).Select((s, i) =>
            $"{i + 1} - {s.Inicio:HH:mm}"));

        var msg = cfg.MsgHorariosDisponiveis.Replace("{data}", data.Value.ToString("dd/MM")) +
                  $"\n\n{listaHorarios}\n\n_0 - Outra data_";

        await Responder(tenantId, telefone, msg, cfg, estadoAntes, "aguardando_horario");
    }

    //  ESTADO: aguardando_horario 

    private async Task ProcessarEscolhaHorario(BotConversa conv, string texto, BotConfig cfg,
        Guid tenantId, string telefone, string estadoAntes, string clinicaNome)
    {
        if (texto == "0")
        {
            conv.Estado        = "aguardando_data";
            conv.DataEscolhida = null;
            await Responder(tenantId, telefone,
                "Ok! Para qual data? (ex: amanha, 25/06)",
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

        var slot = slots[num - 1];

        // Encontra pet do tutor
        Guid? petId = null;
        if (conv.TutorId.HasValue)
        {
            var pet = await _db.Pets
                .Where(p => p.TutorId == conv.TutorId.Value && p.TenantId == tenantId && p.Ativo)
                .OrderBy(p => p.Nome)
                .FirstOrDefaultAsync();
            petId = pet?.Id;
        }

        if (petId == null)
        {
            // Tutor no cadastrado  cria agendamento pendente sem pet
            // A clnica completa depois
            var ag = new Agendamento
            {
                Id         = Guid.NewGuid(),
                TenantId   = tenantId,
                PetId      = await GetPetPlaceholder(tenantId),
                Tipo       = conv.ServicoNome?.ToLower().Replace(" ", "_") ?? "servico",
                DataHora   = slot.Inicio,
                DuracaoMin = conv.DuracaoMin ?? 60,
                Status     = "pendente",
                Origem     = "bot_whatsapp",
                Obs        = $"Agendado via bot. Telefone: {telefone}. Sem cadastro encontrado.",
                CriadoEm   = DateTime.UtcNow
            };
            _db.Agendamentos.Add(ag);
        }
        else
        {
            var ag = new Agendamento
            {
                Id         = Guid.NewGuid(),
                TenantId   = tenantId,
                PetId      = petId.Value,
                Tipo       = conv.ServicoNome?.ToLower().Replace(" ", "_") ?? "servico",
                DataHora   = slot.Inicio,
                DuracaoMin = conv.DuracaoMin ?? 60,
                Status     = "pendente",
                Origem     = "bot_whatsapp",
                CriadoEm   = DateTime.UtcNow
            };
            _db.Agendamentos.Add(ag);
        }

        conv.Estado = "concluido";

        // Notifica a clnica via WhatsApp
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant?.WhatsappNumber != null)
        {
            await _wa.EnviarTexto(tenant.WhatsappNumber,
                $" Novo agendamento via bot!\n\n" +
                $"Servico: {conv.ServicoNome}\n" +
                $"Data: {slot.Inicio:dd/MM/yyyy} as {slot.Inicio:HH:mm}\n" +
                $"Telefone: {telefone}\n\n" +
                $"Acesse o sistema para confirmar.");
        }

        var msg = cfg.MsgConfirmacao
            .Replace("{pet}",     conv.PetNomeDigitado ?? "seu pet")
            .Replace("{servico}", conv.ServicoNome ?? "")
            .Replace("{data}",    slot.Inicio.ToString("dd/MM/yyyy"))
            .Replace("{hora}",    slot.Inicio.ToString("HH:mm"));

        await Responder(tenantId, telefone, msg, cfg, estadoAntes, "concluido");
    }

    //  Helpers 

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
        await _wa.EnviarTexto(telefone, mensagem);

        _db.BotLogs.Add(new BotLog
        {
            Id          = Guid.NewGuid(),
            TenantId    = tenantId,
            Telefone    = telefone,
            Direcao     = "saida",
            Mensagem    = mensagem,
            EstadoAntes = estadoAntes,
            EstadoApos  = estadoApos,
            CriadoEm    = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // Retorna um pet placeholder (primeiro pet ativo do tenant) para no quebrar FK
    private async Task<Guid> GetPetPlaceholder(Guid tenantId)
    {
        var pet = await _db.Pets
            .Where(p => p.TenantId == tenantId && p.Ativo)
            .OrderBy(p => p.CriadoEm)
            .FirstOrDefaultAsync();
        return pet?.Id ?? throw new InvalidOperationException("Nenhum pet cadastrado no tenant.");
    }
}

