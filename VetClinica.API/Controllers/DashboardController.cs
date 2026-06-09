using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public DashboardController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Resumo()
    {
        var hoje     = DateTime.Today;
        var amanha   = hoje.AddDays(1);
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);
        var fimMesAnterior    = inicioMes.AddDays(-1);

        // ── Clinica ──────────────────────────────────────────────────────────
        var agendamentosHoje = await _db.Agendamentos
            .CountAsync(a => a.TenantId == _t.TenantId
                && a.DataHora >= hoje && a.DataHora < amanha
                && a.Status != "cancelado");

        var agendamentosPendentes = await _db.Agendamentos
            .CountAsync(a => a.TenantId == _t.TenantId && a.Status == "pendente");

        var osAbertas = await _db.OrdensServico
            .CountAsync(o => o.TenantId == _t.TenantId
                && o.Status != "entregue" && o.Status != "cancelado");

        var totalPets    = await _db.Pets.CountAsync(p => p.TenantId == _t.TenantId && p.Ativo);
        var totalTutores = await _db.Tutores.CountAsync(t => t.TenantId == _t.TenantId && t.Ativo);

        var limiteVacina  = DateOnly.FromDateTime(hoje.AddDays(30));
        var vacinasVencendo = await _db.Vacinacoes
            .CountAsync(v => v.ProximaDose != null
                && v.ProximaDose <= limiteVacina
                && v.ProximaDose >= DateOnly.FromDateTime(hoje));

        // ── Financeiro ───────────────────────────────────────────────────────
        var vendasMes = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId
                && v.Status == "finalizada" && v.CriadoEm >= inicioMes)
            .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

        var vendasMesAnterior = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId
                && v.Status == "finalizada"
                && v.CriadoEm >= inicioMesAnterior
                && v.CriadoEm < inicioMes)
            .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

        var receitaMes = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId
                && c.Tipo == "receita"
                && (c.Status == "paga" || c.Status == "recebida")
                && c.DataBaixa >= DateOnly.FromDateTime(inicioMes))
            .SumAsync(c => (decimal?)c.ValorPago) ?? 0;

        var despesaMes = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId
                && c.Tipo == "despesa"
                && c.Status == "paga"
                && c.DataBaixa >= DateOnly.FromDateTime(inicioMes))
            .SumAsync(c => (decimal?)c.ValorPago) ?? 0;

        var contasVencer = await _db.Contas
            .CountAsync(c => c.TenantId == _t.TenantId
                && c.Status == "aberta"
                && c.DataVencimento <= DateOnly.FromDateTime(hoje.AddDays(7)));

        var contasVencidas = await _db.Contas
            .CountAsync(c => c.TenantId == _t.TenantId
                && c.Status == "aberta"
                && c.DataVencimento < DateOnly.FromDateTime(hoje));

        // ── Estoque ──────────────────────────────────────────────────────────
        var produtosAbaixoMinimo = await _db.Produtos
            .CountAsync(p => p.TenantId == _t.TenantId && p.Ativo
                && p.EstoqueAtual <= p.EstoqueMinimo);

        // ── Grafico: vendas dos ultimos 6 meses ──────────────────────────────
        var vendasPorMes = new List<object>();
        for (int i = 5; i >= 0; i--)
        {
            var mes     = inicioMes.AddMonths(-i);
            var fimMes  = mes.AddMonths(1);
            var total   = await _db.Vendas
                .Where(v => v.TenantId == _t.TenantId
                    && v.Status == "finalizada"
                    && v.CriadoEm >= mes && v.CriadoEm < fimMes)
                .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;
            vendasPorMes.Add(new {
                mes   = mes.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")),
                total = Math.Round(total, 2)
            });
        }

        // ── Grafico: agendamentos por status (hoje) ──────────────────────────
        var agsPorStatus = await _db.Agendamentos
            .Where(a => a.TenantId == _t.TenantId
                && a.DataHora >= hoje && a.DataHora < amanha)
            .GroupBy(a => a.Status)
            .Select(g => new { status = g.Key, total = g.Count() })
            .ToListAsync();

        // ── Ultimos agendamentos ──────────────────────────────────────────────
        var proximosAgs = await _db.Agendamentos
            .Include(a => a.Pet)
            .Where(a => a.TenantId == _t.TenantId
                && a.DataHora >= hoje
                && a.Status != "cancelado")
            .OrderBy(a => a.DataHora)
            .Take(5)
            .Select(a => new {
                a.Id,
                PetNome   = a.Pet!.Nome,
                a.Tipo,
                a.DataHora,
                a.Status
            })
            .ToListAsync();

        var crescimentoVendas = vendasMesAnterior > 0
            ? Math.Round((vendasMes - vendasMesAnterior) / vendasMesAnterior * 100, 1)
            : 0;

        return Ok(new
        {
            // KPIs
            agendamentosHoje,
            agendamentosPendentes,
            osAbertas,
            totalPets,
            totalTutores,
            vacinasVencendo,
            produtosAbaixoMinimo,
            contasVencer,
            contasVencidas,

            // Financeiro
            vendasMes,
            vendasMesAnterior,
            crescimentoVendas,
            receitaMes,
            despesaMes,
            lucroMes = receitaMes - despesaMes,

            // Graficos
            vendasPorMes,
            agsPorStatus,
            proximosAgs
        });
    }
}
