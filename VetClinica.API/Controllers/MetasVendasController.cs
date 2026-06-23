using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

public record MetaVendaCreate(int Ano, int Mes, string Tipo, decimal ValorMeta);

[ApiController]
[Authorize]
[Route("api/metas-vendas")]
public class MetasVendasController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public MetasVendasController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    private static readonly string[] TiposValidos = { "produto", "servico", "ambos" };

    // ── Realizado: Produto vem de Vendas (PDV), Servico vem de OrdensServico.
    // Nao usa Contas/receita generico de proposito -- aqui e especificamente
    // sobre venda de produto e execucao de servico, nao financeiro geral.
    private async Task<decimal> CalcularRealizado(string tipo, DateOnly de, DateOnly ate)
    {
        var deDt  = de.ToDateTime(TimeOnly.MinValue);
        var ateDt = ate.ToDateTime(TimeOnly.MinValue);

        decimal produto = 0, servico = 0;

        if (tipo is "produto" or "ambos")
            produto = await _db.Vendas
                .Where(v => v.TenantId == _t.TenantId && v.Status == "finalizada"
                         && v.FinalizadaEm >= deDt && v.FinalizadaEm < ateDt)
                .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

        if (tipo is "servico" or "ambos")
            servico = await _db.OrdensServico
                .Where(o => o.TenantId == _t.TenantId && o.Status == "entregue"
                         && o.EntregueEm >= deDt && o.EntregueEm < ateDt)
                .SumAsync(o => (decimal?)(o.ValorTotal ?? 0)) ?? 0;

        return produto + servico;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? ano)
    {
        var a = ano ?? DateTime.Today.Year;
        var metas = await _db.MetasFaturamento
            .Where(m => m.TenantId == _t.TenantId && m.Ano == a)
            .OrderBy(m => m.Mes).ThenBy(m => m.Tipo)
            .ToListAsync();

        var resultado = new List<object>();
        foreach (var m in metas)
        {
            var de  = new DateOnly(m.Ano, m.Mes, 1);
            var ate = de.AddMonths(1);
            var realizado = await CalcularRealizado(m.Tipo, de, ate);
            var pct = m.ValorMeta > 0 ? Math.Round(realizado / m.ValorMeta * 100, 1) : 0;

            resultado.Add(new {
                m.Id, m.Ano, m.Mes, m.Tipo, m.ValorMeta,
                realizado, percentual = pct, atingido = realizado >= m.ValorMeta && m.ValorMeta > 0
            });
        }
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Salvar(MetaVendaCreate dto)
    {
        if (dto.Mes < 1 || dto.Mes > 12) return BadRequest(new { erro = "Mês inválido." });
        if (dto.ValorMeta < 0) return BadRequest(new { erro = "Valor inválido." });
        if (!TiposValidos.Contains(dto.Tipo)) return BadRequest(new { erro = "Tipo deve ser 'produto', 'servico' ou 'ambos'." });

        // Upsert por (ano, mes, tipo) -- evita duplicar a mesma combinacao
        var meta = await _db.MetasFaturamento.FirstOrDefaultAsync(
            m => m.TenantId == _t.TenantId && m.Ano == dto.Ano && m.Mes == dto.Mes && m.Tipo == dto.Tipo);

        if (meta == null)
        {
            meta = new MetaFaturamento
            {
                Id = Guid.NewGuid(), TenantId = _t.TenantId,
                Ano = dto.Ano, Mes = dto.Mes, Tipo = dto.Tipo, ValorMeta = dto.ValorMeta,
                CriadoEm = DateTime.UtcNow, AtualizadoEm = DateTime.UtcNow
            };
            _db.MetasFaturamento.Add(meta);
        }
        else
        {
            meta.ValorMeta = dto.ValorMeta;
            meta.AtualizadoEm = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { meta.Id, meta.Ano, meta.Mes, meta.Tipo, meta.ValorMeta });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(Guid id)
    {
        var meta = await _db.MetasFaturamento.FirstOrDefaultAsync(m => m.Id == id && m.TenantId == _t.TenantId);
        if (meta == null) return NotFound();
        _db.MetasFaturamento.Remove(meta);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
