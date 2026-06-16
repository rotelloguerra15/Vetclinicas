using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public DashboardController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    [HttpGet]
    public async Task<IActionResult> Resumo([FromQuery] int? ano, [FromQuery] int? mes)
    {
        var hoje    = DateTime.Today;
        var anoRef  = ano  ?? hoje.Year;
        var mesRef  = mes  ?? hoje.Month;
        var inicioMes = new DateTime(anoRef, mesRef, 1);
        var fimMes    = inicioMes.AddMonths(1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);

        // ── Clinica ────────────────────────────────────────────────────────
        var amanha = hoje.AddDays(1);
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

        // ── Financeiro do mes de referencia ───────────────────────────────
        var vendasMes = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId
                && v.Status == "finalizada"
                && v.CriadoEm >= inicioMes && v.CriadoEm < fimMes)
            .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

        var vendasMesAnterior = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId
                && v.Status == "finalizada"
                && v.CriadoEm >= inicioMesAnterior && v.CriadoEm < inicioMes)
            .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

        var receitaMes = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId && c.Tipo == "receita"
                && (c.Status == "paga" || c.Status == "recebida")
                && c.DataBaixa >= DateOnly.FromDateTime(inicioMes)
                && c.DataBaixa < DateOnly.FromDateTime(fimMes))
            .SumAsync(c => (decimal?)c.ValorPago) ?? 0;

        var despesaMes = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId && c.Tipo == "despesa"
                && c.Status == "paga"
                && c.DataBaixa >= DateOnly.FromDateTime(inicioMes)
                && c.DataBaixa < DateOnly.FromDateTime(fimMes))
            .SumAsync(c => (decimal?)c.ValorPago) ?? 0;

        var contasVencer = await _db.Contas
            .CountAsync(c => c.TenantId == _t.TenantId && c.Status == "aberta"
                && c.DataVencimento <= DateOnly.FromDateTime(hoje.AddDays(7)));

        var contasVencidas = await _db.Contas
            .CountAsync(c => c.TenantId == _t.TenantId && c.Status == "aberta"
                && c.DataVencimento < DateOnly.FromDateTime(hoje));

        var produtosAbaixoMinimo = await _db.Produtos
            .CountAsync(p => p.TenantId == _t.TenantId && p.Ativo
                && p.EstoqueAtual <= p.EstoqueMinimo);

        // ── Metas x Realizado (6 meses) ───────────────────────────────────
        var metasRealizado = new List<object>();
        for (int i = 5; i >= 0; i--)
        {
            var m       = inicioMes.AddMonths(-i);
            var fimM    = m.AddMonths(1);
            var realiz  = await _db.Vendas
                .Where(v => v.TenantId == _t.TenantId
                    && v.Status == "finalizada"
                    && v.CriadoEm >= m && v.CriadoEm < fimM)
                .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

            var meta = await _db.MetasFaturamento
                .Where(mt => mt.TenantId == _t.TenantId && mt.Ano == m.Year && mt.Mes == m.Month)
                .Select(mt => (decimal?)mt.ValorMeta)
                .FirstOrDefaultAsync() ?? 0;

            var pct = meta > 0 ? Math.Round(realiz / meta * 100, 1) : 0;
            metasRealizado.Add(new {
                mes        = m.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")),
                meta       = Math.Round(meta, 2),
                realizado  = Math.Round(realiz, 2),
                percentual = pct
            });
        }

        // ── Meta do mes atual ────────────────────────────────────────────
        var metaMes = await _db.MetasFaturamento
            .Where(mt => mt.TenantId == _t.TenantId && mt.Ano == anoRef && mt.Mes == mesRef)
            .Select(mt => (decimal?)mt.ValorMeta)
            .FirstOrDefaultAsync() ?? 0;

        // ── Aniversariantes do dia ────────────────────────────────────────
        var diaHoje = (short)hoje.Day;
        var mesHoje = (short)hoje.Month;

        var anivTutores = await _db.Tutores
            .Where(t => t.TenantId == _t.TenantId && t.Ativo
                && t.AniversarioDia == diaHoje && t.AniversarioMes == mesHoje)
            .Select(t => new { t.Id, t.Nome, t.Telefone, tipo = "tutor" })
            .ToListAsync();

        var anivPets = await _db.Pets
            .Include(p => p.Tutor)
            .Where(p => p.TenantId == _t.TenantId && p.Ativo
                && p.DataNascimento != null
                && p.DataNascimento!.Value.Day   == hoje.Day
                && p.DataNascimento!.Value.Month == hoje.Month)
            .Select(p => new {
                p.Id, p.Nome, p.Especie,
                TutorNome   = p.Tutor!.Nome,
                TutorFone   = p.Tutor!.Telefone,
                tipo        = "pet"
            })
            .ToListAsync();

        // ── Grafico agendamentos por status hoje ─────────────────────────
        var agsPorStatus = await _db.Agendamentos
            .Where(a => a.TenantId == _t.TenantId
                && a.DataHora >= hoje && a.DataHora < amanha)
            .GroupBy(a => a.Status)
            .Select(g => new { status = g.Key, total = g.Count() })
            .ToListAsync();

        // ── Proximos agendamentos ─────────────────────────────────────────
        var proximosAgs = await _db.Agendamentos
            .Include(a => a.Pet)
            .Where(a => a.TenantId == _t.TenantId
                && a.DataHora >= hoje && a.Status != "cancelado")
            .OrderBy(a => a.DataHora)
            .Take(5)
            .Select(a => new {
                a.Id,
                PetNome  = a.Pet!.Nome,
                a.Tipo,
                a.DataHora,
                a.Status
            })
            .ToListAsync();

        var crescimento = vendasMesAnterior > 0
            ? Math.Round((vendasMes - vendasMesAnterior) / vendasMesAnterior * 100, 1)
            : 0;

        return Ok(new
        {
            // Referencia
            anoRef, mesRef,

            // KPIs
            agendamentosHoje, agendamentosPendentes, osAbertas,
            totalPets, totalTutores, vacinasVencendo,
            produtosAbaixoMinimo, contasVencer, contasVencidas,

            // Financeiro
            vendasMes, vendasMesAnterior, crescimentoVendas = crescimento,
            receitaMes, despesaMes, lucroMes = receitaMes - despesaMes,
            metaMes,
            percentualMeta = metaMes > 0 ? Math.Round(vendasMes / metaMes * 100, 1) : 0,

            // Graficos
            metasRealizado,
            agsPorStatus,
            proximosAgs,

            // Aniversariantes
            aniversariantesTutores = anivTutores,
            aniversariantesPets    = anivPets,
            totalAniversariantes   = anivTutores.Count + anivPets.Count
        });
    }

    // ── Salvar meta ───────────────────────────────────────────────────────
    public record MetaUpdate(int Ano, int Mes, decimal ValorMeta);

    [HttpPut("meta")]
    public async Task<IActionResult> SalvarMeta(MetaUpdate dto)
    {
        var meta = await _db.MetasFaturamento
            .FirstOrDefaultAsync(m => m.TenantId == _t.TenantId && m.Ano == dto.Ano && m.Mes == dto.Mes);

        if (meta == null)
        {
            meta = new MetaFaturamento
            {
                Id       = Guid.NewGuid(),
                TenantId = _t.TenantId,
                Ano      = dto.Ano,
                Mes      = dto.Mes,
                CriadoEm = DateTime.UtcNow
            };
            _db.MetasFaturamento.Add(meta);
        }

        meta.ValorMeta    = dto.ValorMeta;
        meta.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
