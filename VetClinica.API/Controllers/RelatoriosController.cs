using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/relatorios")]
public class RelatoriosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public RelatoriosController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet("mensal")]
    public async Task<IActionResult> Mensal([FromQuery] int? ano, [FromQuery] int? mes)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        var de = new DateOnly(a, m, 1);
        var ate = de.AddMonths(1);

        var receitas = await _db.Lancamentos
            .Where(l => l.TenantId == _t.TenantId && l.Tipo == "receita" && l.Data >= de && l.Data < ate)
            .SumAsync(l => (decimal?)l.Valor) ?? 0;
        var despesas = await _db.Lancamentos
            .Where(l => l.TenantId == _t.TenantId && l.Tipo == "despesa" && l.Data >= de && l.Data < ate)
            .SumAsync(l => (decimal?)l.Valor) ?? 0;

        var inicioUtc = de.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = ate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var atendimentos = await _db.OrdensServico
            .CountAsync(o => o.TenantId == _t.TenantId && o.CriadoEm >= inicioUtc && o.CriadoEm < fimUtc);
        var vendas = await _db.Vendas
            .CountAsync(v => v.TenantId == _t.TenantId && v.Status == "finalizada" && v.CriadoEm >= inicioUtc && v.CriadoEm < fimUtc);
        var novosClientes = await _db.Tutores
            .CountAsync(t => t.TenantId == _t.TenantId && t.CriadoEm >= inicioUtc && t.CriadoEm < fimUtc);

        return Ok(new RelatorioMensal(a, m, receitas, despesas, receitas - despesas,
            atendimentos, vendas, novosClientes));
    }

    [HttpGet("vendas-por-dia")]
    public async Task<IActionResult> VendasPorDia([FromQuery] int? ano, [FromQuery] int? mes)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        var inicioUtc = new DateTime(a, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimUtc = inicioUtc.AddMonths(1);

        var vendas = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId && v.Status == "finalizada"
                     && v.CriadoEm >= inicioUtc && v.CriadoEm < fimUtc)
            .GroupBy(v => v.CriadoEm.Date)
            .Select(g => new { Data = g.Key, Qtd = g.Count(), Total = g.Sum(v => v.ValorTotal) })
            .OrderBy(x => x.Data)
            .ToListAsync();

        var os = await _db.OrdensServico
            .Where(o => o.TenantId == _t.TenantId && o.EntregueEm != null
                     && o.EntregueEm >= inicioUtc && o.EntregueEm < fimUtc)
            .GroupBy(o => o.EntregueEm!.Value.Date)
            .Select(g => new { Data = g.Key, Total = g.Sum(o => o.ValorTotal ?? 0) })
            .ToListAsync();

        // mescla vendas + OS por dia
        var dias = Enumerable.Range(1, DateTime.DaysInMonth(a, m))
            .Select(d => {
                var data = new DateOnly(a, m, d);
                var dt = data.ToDateTime(TimeOnly.MinValue);
                var vd = vendas.FirstOrDefault(v => v.Data == dt);
                var od = os.FirstOrDefault(o => o.Data == dt);
                return new { data, vendas = vd?.Total ?? 0, servicos = od?.Total ?? 0,
                    total = (vd?.Total ?? 0) + (od?.Total ?? 0) };
            }).ToList();

        return Ok(dias);
    }
}
