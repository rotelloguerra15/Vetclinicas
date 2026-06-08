using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

public record ComprasStatusUpdate(string Status);

[ApiController]
[Authorize]
[Route("api/compras")]
public class ComprasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public ComprasController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.PedidosCompra.Include(p => p.Fornecedor)
            .Where(p => p.TenantId == _t.TenantId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.DataPedido)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new {
                p.Id, p.Numero, p.Status, p.DataPedido, p.DataRecebimento,
                p.ValorTotal, p.Obs,
                FornecedorNome = p.Fornecedor != null ? p.Fornecedor.Nome : null
            })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalhe(Guid id)
    {
        var p = await _db.PedidosCompra
            .Include(x => x.Fornecedor)
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();
        return Ok(p);
    }

    public record PedidoItemCreate(Guid? ProdutoId, string NomeProduto, decimal Quantidade,
        string Unidade, decimal ValorUnitario, string Uso);

    public record PedidoCreate(Guid? FornecedorId, DateOnly DataPedido, string? Obs,
        List<PedidoItemCreate> Itens);

    [HttpPost]
    public async Task<IActionResult> Criar(PedidoCreate dto)
    {
        var pedido = new PedidoCompra
        {
            Id          = Guid.NewGuid(),
            TenantId    = _t.TenantId,
            FornecedorId = dto.FornecedorId,
            Status      = "rascunho",
            DataPedido  = dto.DataPedido,
            Obs         = dto.Obs,
            CriadoPor   = _t.UserId,
            CriadoEm    = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        foreach (var item in dto.Itens)
        {
            var nomeProduto = item.NomeProduto;
            if (string.IsNullOrEmpty(nomeProduto) && item.ProdutoId.HasValue)
            {
                var prod = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == item.ProdutoId.Value);
                nomeProduto = prod?.Nome ?? "Produto";
            }
            pedido.Itens.Add(new PedidoItem
            {
                Id            = Guid.NewGuid(),
                NomeProduto   = nomeProduto,
                ProdutoId     = item.ProdutoId,
                Quantidade    = item.Quantidade,
                Unidade       = item.Unidade,
                ValorUnitario = item.ValorUnitario,
                ValorTotal    = item.Quantidade * item.ValorUnitario,
                Uso           = item.Uso,
                CriadoEm      = DateTime.UtcNow
            });
        }

        pedido.ValorTotal = pedido.Itens.Sum(i => i.ValorTotal);
        _db.PedidosCompra.Add(pedido);
        await _db.SaveChangesAsync();
        return Ok(new { pedido.Id, pedido.Numero });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] ComprasStatusUpdate dto)
    {
        var p = await _db.PedidosCompra.Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();

        var statusAnterior = p.Status;
        p.Status = dto.Status;
        p.AtualizadoEm = DateTime.UtcNow;

        // Ao receber: alimenta o estoque com os itens de venda e uso interno
        if (dto.Status == "recebido" && statusAnterior != "recebido")
        {
            p.DataRecebimento = DateOnly.FromDateTime(DateTime.Today);

            foreach (var item in p.Itens.Where(i => i.ProdutoId.HasValue))
            {
                var produto = await _db.Produtos
                    .FirstOrDefaultAsync(pr => pr.Id == item.ProdutoId!.Value);
                if (produto != null)
                {
                    produto.EstoqueAtual += item.Quantidade;
                    _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                    {
                        Id         = Guid.NewGuid(),
                        TenantId   = _t.TenantId,
                        ProdutoId  = produto.Id,
                        Tipo       = "entrada",
                        Quantidade = item.Quantidade,
                        EstoqueApos = produto.EstoqueAtual,
                        Motivo     = $"Pedido de compra #{p.Numero}"
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var p = await _db.PedidosCompra.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();
        if (p.Status == "recebido")
            return BadRequest(new { erro = "Pedido ja recebido nao pode ser cancelado." });
        p.Status = "cancelado";
        p.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
