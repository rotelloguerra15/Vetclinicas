using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/bot")]
public class BotConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public BotConfigController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    // ── Configuração ──────────────────────────────────────────────────────────

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var cfg = await _db.BotConfigs.FirstOrDefaultAsync(b => b.TenantId == _t.TenantId);
        if (cfg == null) return NotFound();
        return Ok(cfg);
    }

    [HttpPut("config")]
    public async Task<IActionResult> SalvarConfig([FromBody] BotConfig dto)
    {
        var cfg = await _db.BotConfigs.FirstOrDefaultAsync(b => b.TenantId == _t.TenantId);
        if (cfg == null) return NotFound();

        cfg.Ativo                  = dto.Ativo;
        cfg.HoraInicio             = dto.HoraInicio;
        cfg.HoraFim                = dto.HoraFim;
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
        cfg.AtualizadoEm           = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Logs ──────────────────────────────────────────────────────────────────

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? telefone,
        [FromQuery] string? direcao,
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var q = _db.BotLogs.Where(l => l.TenantId == _t.TenantId).AsQueryable();

        if (!string.IsNullOrEmpty(telefone))
            q = q.Where(l => l.Telefone.Contains(telefone));
        if (!string.IsNullOrEmpty(direcao))
            q = q.Where(l => l.Direcao == direcao);
        if (de.HasValue)
            q = q.Where(l => l.CriadoEm >= de.Value);
        if (ate.HasValue)
            q = q.Where(l => l.CriadoEm <= ate.Value);

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
            totalMensagens     = logs.Count,
            mensagensEntrada   = logs.Count(l => l.Direcao == "entrada"),
            mensagensSaida     = logs.Count(l => l.Direcao == "saida"),
            conversasAtivas    = conversas.Count(c => c.Estado != "concluido" && c.Estado != "cancelado"),
            conversasConcluidas= conversas.Count(c => c.Estado == "concluido"),
            conversasCanceladas= conversas.Count(c => c.Estado == "cancelado"),
            erros              = logs.Count(l => l.Erro != null),
            telefonesUnicos    = logs.Select(l => l.Telefone).Distinct().Count(),
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
