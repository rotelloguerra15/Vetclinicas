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
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);

        var agendamentosHoje = await _db.Agendamentos
            .CountAsync(a => a.TenantId == _t.TenantId && a.DataHora >= hoje && a.DataHora < amanha
                          && a.Status != "cancelado");

        var osAbertas = await _db.OrdensServico
            .CountAsync(o => o.TenantId == _t.TenantId && o.Status != "entregue" && o.Status != "cancelado");

        var limiteVacina = DateOnly.FromDateTime(hoje.AddDays(30));
        var vacinasVencendo = await _db.Vacinacoes
            .CountAsync(v => v.ProximaDose != null && v.ProximaDose <= limiteVacina
                          && v.ProximaDose >= DateOnly.FromDateTime(hoje));

        var totalPets = await _db.Pets.CountAsync(p => p.TenantId == _t.TenantId && p.Ativo);
        var totalTutores = await _db.Tutores.CountAsync(t => t.TenantId == _t.TenantId && t.Ativo);

        var produtosAbaixoMinimo = await _db.Produtos
            .CountAsync(p => p.TenantId == _t.TenantId && p.Ativo && p.EstoqueAtual <= p.EstoqueMinimo);

        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var vendasMes = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId && v.Status == "finalizada" && v.CriadoEm >= inicioMes)
            .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

        return Ok(new
        {
            agendamentosHoje,
            osAbertas,
            vacinasVencendo,
            totalPets,
            totalTutores,
            produtosAbaixoMinimo,
            vendasMes
        });
    }
}
