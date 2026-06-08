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

    // ── Condicoes de pagamento ────────────────────────────────────────────────

    [HttpGet("condicoes")]
    public async Task<IActionResult> ListarCondicoes()
    {
        var lista = await _db.CondicoesPagamento
            .Where(c => c.TenantId == _t.TenantId && c.Ativo)
            .OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.Parcelas, c.IntervaloDias })
            .ToListAsync();
        return Ok(lista);
    }

    public record CondicaoCreate(string Nome, int Parcelas, int IntervaloDias);

    [HttpPost("condicoes")]
    public async Task<IActionResult> CriarCondicao(CondicaoCreate dto)
    {
        var c = new CondicaoPagamento
        {
            Id           = Guid.NewGuid(),
            TenantId     = _t.TenantId,
            Nome         = dto.Nome,
            Parcelas     = dto.Parcelas,
            IntervaloDias = dto.IntervaloDias,
            CriadoEm    = DateTime.UtcNow
        };
        _db.CondicoesPagamento.Add(c);
        await _db.SaveChangesAsync();
        return Ok(new { c.Id });
    }

    [HttpDelete("condicoes/{id}")]
    public async Task<IActionResult> RemoverCondicao(Guid id)
    {
        var c = await _db.CondicoesPagamento
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (c == null) return NotFound();
        c.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Pedidos de compra ─────────────────────────────────────────────────────

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
                p.Id, p.Numero, p.Status, p.DataPedido, p.ValorTotal,
                p.Parcelas, p.FormaPagamento,
                FornecedorNome = p.Fornecedor != null ? p.Fornecedor.Nome : null,
                CondicaoNome   = p.CondicaoPagamento != null ? p.CondicaoPagamento.Nome : null
            })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalhe(Guid id)
    {
        var p = await _db.PedidosCompra
            .Include(x => x.Fornecedor)
            .Include(x => x.CondicaoPagamento)
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();
        return Ok(p);
    }

    public record PedidoItemCreate(
        Guid? ProdutoId, string NomeProduto, decimal Quantidade,
        string Unidade, decimal ValorUnitario, string Uso);

    public record PedidoCreate(
        Guid? FornecedorId,
        Guid? CondicaoPagamentoId,
        string? FormaPagamento,
        DateOnly DataPedido,
        string? Obs,
        List<PedidoItemCreate> Itens);

    [HttpPost]
    public async Task<IActionResult> Criar(PedidoCreate dto)
    {
        var pedido = new PedidoCompra
        {
            Id                   = Guid.NewGuid(),
            TenantId             = _t.TenantId,
            FornecedorId         = dto.FornecedorId,
            CondicaoPagamentoId  = dto.CondicaoPagamentoId,
            FormaPagamento       = dto.FormaPagamento,
            Status               = "rascunho",
            DataPedido           = dto.DataPedido,
            Obs                  = dto.Obs,
            CriadoPor            = _t.UserId,
            CriadoEm             = DateTime.UtcNow,
            AtualizadoEm         = DateTime.UtcNow
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
                CriadoEm     = DateTime.UtcNow
            });
        }

        pedido.ValorTotal = pedido.Itens.Sum(i => i.ValorTotal);
        _db.PedidosCompra.Add(pedido);
        await _db.SaveChangesAsync();
        return Ok(new { pedido.Id, pedido.Numero });
    }

    /// <summary>
    /// Confirma o pedido e gera os titulos a pagar no financeiro
    /// conforme a condicao de pagamento.
    /// </summary>
    [HttpPut("{id}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id)
    {
        var pedido = await _db.PedidosCompra
            .Include(p => p.Fornecedor)
            .Include(p => p.CondicaoPagamento)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _t.TenantId);

        if (pedido == null) return NotFound();
        if (pedido.Status != "rascunho")
            return BadRequest(new { erro = "Apenas pedidos em rascunho podem ser confirmados." });

        pedido.Status       = "enviado";
        pedido.AtualizadoEm = DateTime.UtcNow;

        // Gera titulos a pagar no financeiro
        var parcelas  = pedido.CondicaoPagamento?.Parcelas ?? pedido.Parcelas;
        var intervalo = pedido.CondicaoPagamento?.IntervaloDias ?? 30;
        var valorParcela = Math.Round(pedido.ValorTotal / parcelas, 2);
        var fornecedorNome = pedido.Fornecedor?.Nome ?? "Fornecedor";

        for (int i = 0; i < parcelas; i++)
        {
            var vencimento = pedido.DataPedido.AddDays(intervalo * (i + 1));
            var descricao  = parcelas > 1
                ? $"Compra #{pedido.Numero} - {fornecedorNome} ({i + 1}/{parcelas})"
                : $"Compra #{pedido.Numero} - {fornecedorNome}";

            // Ajuste na ultima parcela para fechar o valor exato
            var valor = (i == parcelas - 1)
                ? pedido.ValorTotal - (valorParcela * (parcelas - 1))
                : valorParcela;

            _db.Contas.Add(new Conta
            {
                Id              = Guid.NewGuid(),
                TenantId        = _t.TenantId,
                PedidoId        = pedido.Id,
                Tipo            = "despesa",
                Descricao       = descricao,
                Valor           = valor,
                DataCompetencia = pedido.DataPedido,
                DataVencimento  = vencimento,
                FormaPagamento  = pedido.FormaPagamento,
                Status          = "aberta",
                CriadoPor       = _t.UserId,
                CriadoEm       = DateTime.UtcNow,
                AtualizadoEm   = DateTime.UtcNow
            });
        }

        // Cria registro de recebimento pendente automaticamente
        var recebimento = new RecebimentoMercadoria
        {
            Id              = Guid.NewGuid(),
            TenantId        = _t.TenantId,
            PedidoId        = pedido.Id,
            DataRecebimento = pedido.DataPedido.AddDays(7), // estimativa
            Status          = "pendente",
            CriadoPor       = _t.UserId,
            CriadoEm       = DateTime.UtcNow
        };

        // Copia itens do pedido para o recebimento
        var itens = await _db.PedidoItens.Where(i => i.PedidoId == id).ToListAsync();
        foreach (var item in itens)
        {
            recebimento.Itens.Add(new RecebimentoItem
            {
                Id                 = Guid.NewGuid(),
                PedidoItemId       = item.Id,
                ProdutoId          = item.ProdutoId,
                NomeProduto        = item.NomeProduto,
                QuantidadePedida   = item.Quantidade,
                QuantidadeRecebida = 0,
                ValorUnitario      = item.ValorUnitario,
                Uso                = item.Uso,
                CriadoEm          = DateTime.UtcNow
            });
        }

        _db.RecebimentosMercadoria.Add(recebimento);
        await _db.SaveChangesAsync();

        return Ok(new { titulos = parcelas, recebimentoId = recebimento.Id });
    }

    [HttpPut("{id}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var p = await _db.PedidosCompra.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();
        if (p.Status == "recebido")
            return BadRequest(new { erro = "Pedido ja recebido nao pode ser cancelado." });

        p.Status       = "cancelado";
        p.AtualizadoEm = DateTime.UtcNow;

        // Cancela titulos a pagar gerados por este pedido
        var titulos = await _db.Contas.Where(c => c.PedidoId == id && c.Status == "aberta").ToListAsync();
        foreach (var t in titulos) t.Status = "cancelada";

        // Cancela recebimento pendente
        var recebimentos = await _db.RecebimentosMercadoria
            .Where(r => r.PedidoId == id && r.Status == "pendente").ToListAsync();
        foreach (var r in recebimentos) r.Status = "cancelado";

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
