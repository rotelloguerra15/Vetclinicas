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
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public RelatoriosController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    // ── Mensal básico (existente) ─────────────────────────────────────────────

    [HttpGet("mensal")]
    public async Task<IActionResult> Mensal([FromQuery] int? ano, [FromQuery] int? mes)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        var de  = new DateOnly(a, m, 1);
        var ate = de.AddMonths(1);

        var receitas  = await _db.Lancamentos.Where(l => l.TenantId == _t.TenantId && l.Tipo == "receita" && l.Data >= de && l.Data < ate).SumAsync(l => (decimal?)l.Valor) ?? 0;
        var despesas  = await _db.Lancamentos.Where(l => l.TenantId == _t.TenantId && l.Tipo == "despesa" && l.Data >= de && l.Data < ate).SumAsync(l => (decimal?)l.Valor) ?? 0;

        var inicioUtc = de.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc    = ate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var atendimentos  = await _db.OrdensServico.CountAsync(o => o.TenantId == _t.TenantId && o.CriadoEm >= inicioUtc && o.CriadoEm < fimUtc);
        var vendas        = await _db.Vendas.CountAsync(v => v.TenantId == _t.TenantId && v.Status == "finalizada" && v.CriadoEm >= inicioUtc && v.CriadoEm < fimUtc);
        var novosClientes = await _db.Tutores.CountAsync(t => t.TenantId == _t.TenantId && t.CriadoEm >= inicioUtc && t.CriadoEm < fimUtc);

        return Ok(new RelatorioMensal(a, m, receitas, despesas, receitas - despesas, atendimentos, vendas, novosClientes));
    }

    // ── Vendas por dia (existente) ────────────────────────────────────────────

    [HttpGet("vendas-por-dia")]
    public async Task<IActionResult> VendasPorDia([FromQuery] int? ano, [FromQuery] int? mes)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        var inicioUtc = new DateTime(a, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimUtc    = inicioUtc.AddMonths(1);

        var vendas = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId && v.Status == "finalizada" && v.CriadoEm >= inicioUtc && v.CriadoEm < fimUtc)
            .GroupBy(v => v.CriadoEm.Date)
            .Select(g => new { Data = g.Key, Qtd = g.Count(), Total = g.Sum(v => v.ValorTotal) })
            .OrderBy(x => x.Data).ToListAsync();

        var os = await _db.OrdensServico
            .Where(o => o.TenantId == _t.TenantId && o.EntregueEm != null && o.EntregueEm >= inicioUtc && o.EntregueEm < fimUtc)
            .GroupBy(o => o.EntregueEm!.Value.Date)
            .Select(g => new { Data = g.Key, Total = g.Sum(o => o.ValorTotal ?? 0) })
            .ToListAsync();

        var dias = Enumerable.Range(1, DateTime.DaysInMonth(a, m)).Select(d => {
            var data = new DateOnly(a, m, d);
            var dt   = data.ToDateTime(TimeOnly.MinValue);
            var vd   = vendas.FirstOrDefault(v => v.Data == dt);
            var od   = os.FirstOrDefault(o => o.Data == dt);
            return new { data, vendas = vd?.Total ?? 0, servicos = od?.Total ?? 0, total = (vd?.Total ?? 0) + (od?.Total ?? 0) };
        }).ToList();

        return Ok(dias);
    }

    // ── TOP Serviços mais realizados ──────────────────────────────────────────
    // GET /api/relatorios/top-servicos?ano=2026&mes=6&top=10

    [HttpGet("top-servicos")]
    public async Task<IActionResult> TopServicos([FromQuery] int? ano, [FromQuery] int? mes, [FromQuery] int top = 10)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        var inicioUtc = new DateTime(a, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimUtc    = inicioUtc.AddMonths(1);

        var resultado = await _db.OsServicos
            .Join(_db.OrdensServico,
                oss => oss.OsId,
                os  => os.Id,
                (oss, os) => new { oss, os })
            .Join(_db.Servicos,
                x   => x.oss.ServicoId,
                s   => s.Id,
                (x, s) => new { x.oss, x.os, s })
            .Where(x => x.os.TenantId == _t.TenantId
                     && x.os.CriadoEm >= inicioUtc
                     && x.os.CriadoEm < fimUtc)
            .GroupBy(x => new { x.s.Id, x.s.Nome })
            .Select(g => new {
                nome       = g.Key.Nome,
                quantidade = g.Count(),
                total      = g.Sum(x => x.oss.PrecoCobrado)
            })
            .OrderByDescending(x => x.quantidade)
            .Take(top)
            .ToListAsync();

        return Ok(resultado);
    }

    // ── TOP Produtos mais vendidos (PDV) ──────────────────────────────────────
    // GET /api/relatorios/top-produtos?ano=2026&mes=6&top=10

    [HttpGet("top-produtos")]
    public async Task<IActionResult> TopProdutos([FromQuery] int? ano, [FromQuery] int? mes, [FromQuery] int top = 10)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        var inicioUtc = new DateTime(a, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimUtc    = inicioUtc.AddMonths(1);

        var resultado = await _db.VendaItens
            .Join(_db.Vendas,
                vi => vi.VendaId,
                v  => v.Id,
                (vi, v) => new { vi, v })
            .Join(_db.Produtos,
                x  => x.vi.ProdutoId,
                p  => p.Id,
                (x, p) => new { x.vi, x.v, p })
            .Where(x => x.v.TenantId == _t.TenantId
                     && x.v.Status == "finalizada"
                     && x.v.CriadoEm >= inicioUtc
                     && x.v.CriadoEm < fimUtc)
            .GroupBy(x => new { x.p.Id, x.p.Nome, x.p.Categoria })
            .Select(g => new {
                nome       = g.Key.Nome,
                categoria  = g.Key.Categoria,
                quantidade = g.Sum(x => x.vi.Quantidade),
                total      = g.Sum(x => x.vi.Subtotal)
            })
            .OrderByDescending(x => x.total)
            .Take(top)
            .ToListAsync();

        return Ok(resultado);
    }

    // ── Custo por categoria (despesas agrupadas) ──────────────────────────────
    // GET /api/relatorios/custos-categoria?ano=2026&mes=6

    [HttpGet("custos-categoria")]
    public async Task<IActionResult> CustosCategoria([FromQuery] int? ano, [FromQuery] int? mes)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        var de  = new DateOnly(a, m, 1);
        var ate = de.AddMonths(1);

        var resultado = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId
                     && c.Tipo == "despesa"
                     && c.Status == "paga"
                     && c.DataBaixa >= de
                     && c.DataBaixa < ate)
            .GroupBy(c => c.CategoriaId)
            .Select(g => new {
                categoriaId = g.Key,
                total       = g.Sum(c => c.ValorPago ?? 0),
                quantidade  = g.Count()
            })
            .ToListAsync();

        // Enriquece com nome da categoria
        var categorias = await _db.CategoriasFinanceiras
            .Where(c => c.TenantId == _t.TenantId)
            .ToDictionaryAsync(c => c.Id, c => c.Nome);

        var enriquecido = resultado
            .Select(r => new {
                categoria  = r.categoriaId.HasValue && categorias.ContainsKey(r.categoriaId.Value)
                                ? categorias[r.categoriaId.Value]
                                : "Sem categoria",
                total      = r.total,
                quantidade = r.quantidade
            })
            .OrderByDescending(x => x.total)
            .ToList();

        return Ok(enriquecido);
    }

    // ── Receita por categoria ─────────────────────────────────────────────────
    // GET /api/relatorios/receitas-categoria?ano=2026&mes=6

    [HttpGet("receitas-categoria")]
    public async Task<IActionResult> ReceitasCategoria([FromQuery] int? ano, [FromQuery] int? mes)
    {
        var a = ano ?? DateTime.Today.Year;
        var m = mes ?? DateTime.Today.Month;
        var de  = new DateOnly(a, m, 1);
        var ate = de.AddMonths(1);

        var resultado = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId
                     && c.Tipo == "receita"
                     && (c.Status == "recebida" || c.Status == "paga")
                     && c.DataBaixa >= de
                     && c.DataBaixa < ate)
            .GroupBy(c => c.CategoriaId)
            .Select(g => new {
                categoriaId = g.Key,
                total       = g.Sum(c => c.ValorPago ?? 0),
                quantidade  = g.Count()
            })
            .ToListAsync();

        var categorias = await _db.CategoriasFinanceiras
            .Where(c => c.TenantId == _t.TenantId)
            .ToDictionaryAsync(c => c.Id, c => c.Nome);

        var enriquecido = resultado
            .Select(r => new {
                categoria  = r.categoriaId.HasValue && categorias.ContainsKey(r.categoriaId.Value)
                                ? categorias[r.categoriaId.Value]
                                : "Sem categoria",
                total      = r.total,
                quantidade = r.quantidade
            })
            .OrderByDescending(x => x.total)
            .ToList();

        return Ok(enriquecido);
    }

    // ── Evolução mensal (12 meses) ────────────────────────────────────────────
    // GET /api/relatorios/evolucao?meses=12

    // ── Contratos: previsto vs realizado ────────────────────────────────────
    // GET /api/relatorios/contratos-previsto-realizado?mesesPassado=3&mesesFuturo=6
    // "previsto" = tudo que foi planejado por contrato pra cair naquele mes
    // (independente do status atual). "realizado" = o que ja foi efetivamente
    // pago daquele planejado. Os dois agrupados pela DataVencimento original
    // do titulo (nao muda quando a parcela e aprovada/paga).

    [HttpGet("contratos-previsto-realizado")]
    public async Task<IActionResult> ContratosPrevistoRealizado([FromQuery] int mesesPassado = 3, [FromQuery] int mesesFuturo = 6)
    {
        var resultado = new List<object>();

        for (int i = -mesesPassado; i <= mesesFuturo; i++)
        {
            var refMes = DateTime.Today.AddMonths(i);
            var de  = new DateOnly(refMes.Year, refMes.Month, 1);
            var ate = de.AddMonths(1);
            var label = de.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR"));

            var previsto = await _db.Contas
                .Where(c => c.TenantId == _t.TenantId && c.ContratoParcelaId != null
                         && c.DataVencimento >= de && c.DataVencimento < ate)
                .SumAsync(c => (decimal?)c.Valor) ?? 0;

            var realizado = await _db.Contas
                .Where(c => c.TenantId == _t.TenantId && c.ContratoParcelaId != null
                         && c.Status == "paga"
                         && c.DataVencimento >= de && c.DataVencimento < ate)
                .SumAsync(c => (decimal?)(c.ValorPago ?? c.Valor)) ?? 0;

            resultado.Add(new { mes = label, previsto, realizado, pendente = previsto - realizado });
        }

        return Ok(resultado);
    }

    // GET /api/relatorios/contratos-previsto-realizado-fornecedor?mesesPassado=3&mesesFuturo=6
    // Mesmo recorte do endpoint acima, mas agregado por fornecedor em vez de por mes
    // -- pra ver quem concentra mais valor previsto/pendente no contrato.

    [HttpGet("contratos-previsto-realizado-fornecedor")]
    public async Task<IActionResult> ContratosPrevistoRealizadoPorFornecedor([FromQuery] int mesesPassado = 3, [FromQuery] int mesesFuturo = 6)
    {
        var de  = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-mesesPassado);
        var ate = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(mesesFuturo + 1);

        var linhas = await (
            from c in _db.Contas
            join cp in _db.ContratoParcelas on c.ContratoParcelaId equals cp.Id
            join ct in _db.Contratos on cp.ContratoId equals ct.Id
            join f in _db.Fornecedores on ct.FornecedorId equals f.Id
            where c.TenantId == _t.TenantId
               && c.DataVencimento >= de && c.DataVencimento < ate
            select new { f.Id, f.Nome, c.Valor, c.ValorPago, c.Status }
        ).ToListAsync();

        var resultado = linhas
            .GroupBy(x => new { x.Id, x.Nome })
            .Select(g => new {
                fornecedorId = g.Key.Id,
                fornecedor   = g.Key.Nome,
                previsto     = g.Sum(x => x.Valor),
                realizado    = g.Where(x => x.Status == "paga").Sum(x => x.ValorPago ?? x.Valor)
            })
            .Select(x => new { x.fornecedorId, x.fornecedor, x.previsto, x.realizado, pendente = x.previsto - x.realizado })
            .OrderByDescending(x => x.previsto)
            .ToList();

        return Ok(resultado);
    }

    [HttpGet("evolucao")]
    public async Task<IActionResult> Evolucao([FromQuery] int meses = 12)
    {
        var resultado = new List<object>();

        for (int i = meses - 1; i >= 0; i--)
        {
            var ref_    = DateTime.Today.AddMonths(-i);
            var de      = new DateOnly(ref_.Year, ref_.Month, 1);
            var ate     = de.AddMonths(1);
            var label   = de.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR"));

            var receitas = await _db.Contas
                .Where(c => c.TenantId == _t.TenantId && c.Tipo == "receita"
                         && (c.Status == "recebida" || c.Status == "paga")
                         && c.DataBaixa >= de && c.DataBaixa < ate)
                .SumAsync(c => (decimal?)(c.ValorPago ?? 0)) ?? 0;

            var despesas = await _db.Contas
                .Where(c => c.TenantId == _t.TenantId && c.Tipo == "despesa"
                         && c.Status == "paga"
                         && c.DataBaixa >= de && c.DataBaixa < ate)
                .SumAsync(c => (decimal?)(c.ValorPago ?? 0)) ?? 0;

            resultado.Add(new { mes = label, receitas, despesas, saldo = receitas - despesas });
        }

        return Ok(resultado);
    }

    // ── Metas de faturamento ──────────────────────────────────────────────────
    // GET /api/relatorios/metas?ano=2026

    [HttpGet("metas")]
    public async Task<IActionResult> Metas([FromQuery] int? ano)
    {
        var a = ano ?? DateTime.Today.Year;

        var metas = await _db.MetasFaturamento
            .Where(m => m.TenantId == _t.TenantId && m.Ano == a)
            .ToListAsync();

        var resultado = new List<object>();

        for (int m = 1; m <= 12; m++)
        {
            var de  = new DateOnly(a, m, 1);
            var ate = de.AddMonths(1);

            var realizado = await _db.Contas
                .Where(c => c.TenantId == _t.TenantId
                         && c.Tipo == "receita"
                         && (c.Status == "recebida" || c.Status == "paga")
                         && c.DataBaixa >= de && c.DataBaixa < ate)
                .SumAsync(c => (decimal?)(c.ValorPago ?? 0)) ?? 0;

            var meta = metas.FirstOrDefault(x => x.Mes == m)?.ValorMeta ?? 0;
            var pct  = meta > 0 ? Math.Round((realizado / meta) * 100, 1) : 0;
            var label = de.ToString("MMM", new System.Globalization.CultureInfo("pt-BR"));

            resultado.Add(new { mes = m, label, meta, realizado, percentual = pct, atingido = realizado >= meta && meta > 0 });
        }

        return Ok(resultado);
    }

    // ── Salvar / atualizar meta ───────────────────────────────────────────────
    // PUT /api/relatorios/metas/{ano}/{mes}

    public record MetaRequest(decimal ValorMeta);

    [HttpPut("metas/{ano}/{mes}")]
    public async Task<IActionResult> SalvarMeta(int ano, int mes, MetaRequest dto)
    {
        if (mes < 1 || mes > 12) return BadRequest(new { erro = "Mês inválido." });
        if (dto.ValorMeta < 0)   return BadRequest(new { erro = "Valor inválido." });

        var meta = await _db.MetasFaturamento
            .FirstOrDefaultAsync(m => m.TenantId == _t.TenantId && m.Ano == ano && m.Mes == mes);

        if (meta == null)
        {
            meta = new Models.MetaFaturamento
            {
                Id          = Guid.NewGuid(),
                TenantId    = _t.TenantId,
                Ano         = ano,
                Mes         = mes,
                ValorMeta   = dto.ValorMeta,
                CriadoEm    = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };
            _db.MetasFaturamento.Add(meta);
        }
        else
        {
            meta.ValorMeta    = dto.ValorMeta;
            meta.AtualizadoEm = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Meta salva.", ano, mes, valorMeta = dto.ValorMeta });
    }
}
