using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/parametros")]
public class ParametrosController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public ParametrosController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null)
        {
            p = new ParametrosSistema
            {
                Id           = Guid.NewGuid(),
                TenantId     = _t.TenantId,
                ComissaoOsAtivo  = true,
                ComissaoPdvAtivo = false,
                CriadoEm     = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };
            _db.ParametrosSistema.Add(p);
            await _db.SaveChangesAsync();
        }

        return Ok(new { p.Id, p.ComissaoOsAtivo, p.ComissaoPdvAtivo });
    }

    public record ParametrosUpdate(bool ComissaoOsAtivo, bool ComissaoPdvAtivo);

    [HttpPut]
    public async Task<IActionResult> Atualizar(ParametrosUpdate dto)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null)
        {
            p = new ParametrosSistema
            {
                Id       = Guid.NewGuid(),
                TenantId = _t.TenantId,
                CriadoEm = DateTime.UtcNow
            };
            _db.ParametrosSistema.Add(p);
        }

        p.ComissaoOsAtivo  = dto.ComissaoOsAtivo;
        p.ComissaoPdvAtivo = dto.ComissaoPdvAtivo;
        p.AtualizadoEm     = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Bot Config ────────────────────────────────────────────────────────────

    public record BotCfgDto(
        bool Ativo,
        string HoraInicio, string HoraFim, string DiasSemana,
        int DiasAntecedenciaMin, int DiasAntecedenciaMax, int TimeoutConversaMin,
        string MsgBoasVindas, string MsgQualPet, string MsgQualServico,
        string MsgQualData, string MsgHorariosDisponiveis, string MsgConfirmacao,
        string MsgSemHorarios, string MsgForaHorario, string MsgErro, string MsgCancelar,
        string? MetaPhoneNumberId, string? MetaWabaId);

    [HttpGet("~/api/bot-config/config")]
    public async Task<IActionResult> GetBotConfig()
    {
        var cfg = await _db.BotConfigs.FirstOrDefaultAsync(b => b.TenantId == _t.TenantId);
        if (cfg == null) return NotFound();
        return Ok(new {
            cfg.Id, cfg.Ativo,
            horaInicio             = cfg.HoraInicio.ToString("HH:mm"),
            horaFim                = cfg.HoraFim.ToString("HH:mm"),
            cfg.DiasSemana, cfg.DiasAntecedenciaMin, cfg.DiasAntecedenciaMax,
            cfg.TimeoutConversaMin, cfg.MsgBoasVindas, cfg.MsgQualPet,
            cfg.MsgQualServico, cfg.MsgQualData, cfg.MsgHorariosDisponiveis,
            cfg.MsgConfirmacao, cfg.MsgSemHorarios, cfg.MsgForaHorario,
            cfg.MsgErro, cfg.MsgCancelar,
            cfg.MetaPhoneNumberId, cfg.MetaWabaId
        });
    }

    [HttpPost("~/api/bot-config/config/salvar")]
    public async Task<IActionResult> SalvarBotConfig([FromBody] BotCfgDto dto)
    {
        var cfg = await _db.BotConfigs.FirstOrDefaultAsync(b => b.TenantId == _t.TenantId);
        if (cfg == null) return NotFound();

        cfg.Ativo                  = dto.Ativo;
        cfg.HoraInicio             = TimeOnly.Parse(dto.HoraInicio);
        cfg.HoraFim                = TimeOnly.Parse(dto.HoraFim);
        cfg.DiasSemana             = dto.DiasSemana;
        cfg.DiasAntecedenciaMin    = dto.DiasAntecedenciaMin;
        cfg.DiasAntecedenciaMax    = dto.DiasAntecedenciaMax;
        cfg.TimeoutConversaMin     = dto.TimeoutConversaMin;
        cfg.MsgBoasVindas          = dto.MsgBoasVindas;
        cfg.MsgQualPet             = dto.MsgQualPet;
        cfg.MsgQualServico         = dto.MsgQualServico;
        cfg.MsgQualData            = dto.MsgQualData;
        cfg.MsgHorariosDisponiveis = dto.MsgHorariosDisponiveis;
        cfg.MsgConfirmacao         = dto.MsgConfirmacao;
        cfg.MsgSemHorarios         = dto.MsgSemHorarios;
        cfg.MsgForaHorario         = dto.MsgForaHorario;
        cfg.MsgErro                = dto.MsgErro;
        cfg.MsgCancelar            = dto.MsgCancelar;
        cfg.MetaPhoneNumberId      = dto.MetaPhoneNumberId;
        cfg.MetaWabaId             = dto.MetaWabaId;
        cfg.AtualizadoEm           = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { sucesso = true });
    }

    [HttpGet("~/api/bot-config/logs")]
    public async Task<IActionResult> GetBotLogs(
        [FromQuery] string? telefone, [FromQuery] string? direcao,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.BotLogs.Where(l => l.TenantId == _t.TenantId).AsQueryable();
        if (!string.IsNullOrEmpty(telefone)) q = q.Where(l => l.Telefone.Contains(telefone));
        if (!string.IsNullOrEmpty(direcao))  q = q.Where(l => l.Direcao == direcao);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(l => l.CriadoEm)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new { l.Id, l.Telefone, l.Direcao, l.Mensagem, l.EstadoAntes, l.EstadoApos, l.Erro, l.CriadoEm })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("~/api/bot-config/logs/resumo")]
    public async Task<IActionResult> GetBotLogsResumo([FromQuery] int dias = 7)
    {
        var desde = DateTime.UtcNow.AddDays(-dias);
        var logs  = await _db.BotLogs.Where(l => l.TenantId == _t.TenantId && l.CriadoEm >= desde).ToListAsync();
        var convs = await _db.BotConversas.Where(c => c.TenantId == _t.TenantId).ToListAsync();
        return Ok(new {
            totalMensagens      = logs.Count,
            mensagensEntrada    = logs.Count(l => l.Direcao == "entrada"),
            mensagensSaida      = logs.Count(l => l.Direcao == "saida"),
            conversasAtivas     = convs.Count(c => c.Estado != "concluido" && c.Estado != "cancelado"),
            conversasConcluidas = convs.Count(c => c.Estado == "concluido"),
            conversasCanceladas = convs.Count(c => c.Estado == "cancelado"),
            erros               = logs.Count(l => l.Erro != null),
            telefonesUnicos     = logs.Select(l => l.Telefone).Distinct().Count()
        });
    }
}
