using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

// DTO fora da classe para evitar conflito de roteamento
public record BotConfigSalvarDto(
    bool Ativo,
    string HoraInicio, string HoraFim, string DiasSemana,
    int DiasAntecedenciaMin, int DiasAntecedenciaMax, int TimeoutConversaMin,
    string MsgBoasVindas, string MsgQualPet, string MsgQualServico,
    string MsgQualData, string MsgHorariosDisponiveis, string MsgConfirmacao,
    string MsgSemHorarios, string MsgForaHorario, string MsgErro, string MsgCancelar,
    string? MetaPhoneNumberId, string? MetaWabaId);

[ApiController]
[Authorize]
[Route("api/bot-config")]
public class BotConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public BotConfigController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
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

    [HttpPost("config/salvar")]
    public async Task<IActionResult> SalvarConfig([FromBody] BotConfigSalvarDto dto)
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

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? telefone, [FromQuery] string? direcao,
        [FromQuery] DateTime? de, [FromQuery] DateTime? ate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.BotLogs.Where(l => l.TenantId == _t.TenantId).AsQueryable();
        if (!string.IsNullOrEmpty(telefone)) q = q.Where(l => l.Telefone.Contains(telefone));
        if (!string.IsNullOrEmpty(direcao))  q = q.Where(l => l.Direcao == direcao);
        if (de.HasValue)  q = q.Where(l => l.CriadoEm >= de.Value);
        if (ate.HasValue) q = q.Where(l => l.CriadoEm <= ate.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(l => l.CriadoEm)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new {
                l.Id, l.Telefone, l.Direcao, l.Mensagem,
                l.EstadoAntes, l.EstadoApos, l.Erro, l.CriadoEm
            })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("logs/resumo")]
    public async Task<IActionResult> GetResumo([FromQuery] int dias = 7)
    {
        var desde = DateTime.UtcNow.AddDays(-dias);
        var logs  = await _db.BotLogs
            .Where(l => l.TenantId == _t.TenantId && l.CriadoEm >= desde)
            .ToListAsync();
        var conversas = await _db.BotConversas
            .Where(c => c.TenantId == _t.TenantId)
            .ToListAsync();
        return Ok(new {
            totalMensagens      = logs.Count,
            mensagensEntrada    = logs.Count(l => l.Direcao == "entrada"),
            mensagensSaida      = logs.Count(l => l.Direcao == "saida"),
            conversasAtivas     = conversas.Count(c => c.Estado != "concluido" && c.Estado != "cancelado"),
            conversasConcluidas = conversas.Count(c => c.Estado == "concluido"),
            conversasCanceladas = conversas.Count(c => c.Estado == "cancelado"),
            erros               = logs.Count(l => l.Erro != null),
            telefonesUnicos     = logs.Select(l => l.Telefone).Distinct().Count(),
            porDia = logs
                .GroupBy(l => l.CriadoEm.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { data = g.Key.ToString("dd/MM"), total = g.Count() })
                .ToList()
        });
    }

    [HttpDelete("logs")]
    public async Task<IActionResult> LimparLogs([FromQuery] int diasAntigos = 30)
    {
        var limite = DateTime.UtcNow.AddDays(-diasAntigos);
        var count  = await _db.BotLogs
            .Where(l => l.TenantId == _t.TenantId && l.CriadoEm < limite)
            .ExecuteDeleteAsync();
        return Ok(new { removidos = count });
    }
}
