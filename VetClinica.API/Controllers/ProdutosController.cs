using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public ProdutosController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca, [FromQuery] string? categoria)
    {
        var q = _db.Produtos.Where(p => p.TenantId == _t.TenantId && p.Ativo);
        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(p => p.Nome.ToLower().Contains(busca.ToLower()));
        if (!string.IsNullOrWhiteSpace(categoria))
            q = q.Where(p => p.Categoria == categoria);

        var items = await q.OrderBy(p => p.Nome)
            .Select(p => new ProdutoDto(p.Id, p.Nome, p.Categoria, p.Unidade,
                p.PrecoCusto, p.PrecoVenda, p.EstoqueAtual, p.EstoqueMinimo,
                p.EstoqueIdeal, p.EstoqueAtual <= p.EstoqueMinimo))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(ProdutoCreate dto)
    {
        var p = new Produto
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Nome = dto.Nome,
            Codigo = dto.Codigo,
            CodigoBarras = dto.CodigoBarras,
            Categoria = dto.Categoria,
            Unidade = dto.Unidade,
            PrecoCusto = dto.PrecoCusto,
            PrecoVenda = dto.PrecoVenda,
            EstoqueAtual = dto.EstoqueAtual,
            EstoqueMinimo = dto.EstoqueMinimo,
            EstoqueIdeal = dto.EstoqueIdeal,
            ControlaValidade = dto.ControlaValidade,
            CriadoEm = DateTime.UtcNow
        };
        _db.Produtos.Add(p);
        await _db.SaveChangesAsync(); // salva produto primeiro para garantir FK

        // movimentacao inicial de entrada (se houver estoque)
        if (dto.EstoqueAtual > 0)
        {
            _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                Id = Guid.NewGuid(), TenantId = _t.TenantId, ProdutoId = p.Id,
                Tipo = "entrada", Quantidade = dto.EstoqueAtual, EstoqueApos = dto.EstoqueAtual,
                CustoUnitario = dto.PrecoCusto, Motivo = "Cadastro inicial",
                UserId = _t.UserId, Data = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        return Ok(new { p.Id });
    }

    // Ajuste manual: entrada (compra), avaria, validade, consumo, etc.
    [HttpPost("{id}/ajuste")]
    public async Task<IActionResult> Ajustar(Guid id, AjusteEstoque dto)
    {
        var p = await _db.Produtos.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();

        // entrada soma, demais subtraem
        var delta = dto.Tipo == "entrada" ? dto.Quantidade : -Math.Abs(dto.Quantidade);
        p.EstoqueAtual += delta;

        _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
        {
            Id = Guid.NewGuid(), TenantId = _t.TenantId, ProdutoId = p.Id,
            Tipo = dto.Tipo, Quantidade = delta, EstoqueApos = p.EstoqueAtual,
            Motivo = dto.Motivo, UserId = _t.UserId, Data = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { p.EstoqueAtual });
    }

    // Kardex do produto
    [HttpGet("{id}/movimentacoes")]
    public async Task<IActionResult> Movimentacoes(Guid id)
    {
        var movs = await _db.MovimentacoesEstoque
            .Where(m => m.ProdutoId == id && m.TenantId == _t.TenantId)
            .OrderByDescending(m => m.Data)
            .Select(m => new { m.Tipo, m.Quantidade, m.EstoqueApos, m.Motivo, m.Data })
            .ToListAsync();
        return Ok(movs);
    }

    // Sugestão de compra: produtos no/abaixo do mínimo
    [HttpGet("sugestao-compra")]
    public async Task<IActionResult> SugestaoCompra()
    {
        var sugestoes = await _db.Produtos
            .Where(p => p.TenantId == _t.TenantId && p.Ativo && p.EstoqueAtual <= p.EstoqueMinimo)
            .OrderByDescending(p => p.EstoqueMinimo - p.EstoqueAtual)
            .Select(p => new SugestaoCompra(p.Id, p.Nome, p.EstoqueAtual, p.EstoqueMinimo,
                (p.EstoqueIdeal ?? p.EstoqueMinimo) - p.EstoqueAtual))
            .ToListAsync();
        return Ok(sugestoes);
    }
}
