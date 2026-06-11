using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/recebimentos")]
public class RecebimentoController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public RecebimentoController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? status)
    {
        var q = _db.RecebimentosMercadoria
            .Include(r => r.Pedido).ThenInclude(p => p!.Fornecedor)
            .Where(r => r.TenantId == _t.TenantId);

        if (!string.IsNullOrEmpty(status)) q = q.Where(r => r.Status == status);

        var items = await q.OrderByDescending(r => r.CriadoEm)
            .Select(r => new {
                r.Id, r.Status, r.DataRecebimento, r.Obs,
                PedidoNumero   = r.Pedido != null ? r.Pedido.Numero : (int?)null,
                FornecedorNome = r.Pedido != null && r.Pedido.Fornecedor != null
                    ? r.Pedido.Fornecedor.Nome : null
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalhe(Guid id)
    {
        var r = await _db.RecebimentosMercadoria
            .Include(x => x.Pedido).ThenInclude(p => p!.Fornecedor)
            .Include(x => x.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (r == null) return NotFound();
        return Ok(r);
    }

    public record ItemRecebidoUpdate(Guid ItemId, decimal QuantidadeRecebida);

    /// <summary>
    /// Atualiza as quantidades recebidas de cada item (conferencia fisica).
    /// </summary>
    [HttpPut("{id}/itens")]
    public async Task<IActionResult> AtualizarItens(Guid id, List<ItemRecebidoUpdate> itens)
    {
        var recebimento = await _db.RecebimentosMercadoria
            .Include(r => r.Itens)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _t.TenantId);
        if (recebimento == null) return NotFound();
        if (recebimento.Status == "finalizado")
            return BadRequest(new { erro = "Recebimento ja finalizado." });

        foreach (var upd in itens)
        {
            var item = recebimento.Itens.FirstOrDefault(i => i.Id == upd.ItemId);
            if (item != null)
                item.QuantidadeRecebida = upd.QuantidadeRecebida;
        }

        recebimento.Status = "conferido";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Finaliza o recebimento: alimenta o estoque com as quantidades recebidas.
    /// </summary>
    [HttpPut("{id}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id)
    {
        var recebimento = await _db.RecebimentosMercadoria
            .Include(r => r.Itens)
            .Include(r => r.Pedido)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _t.TenantId);

        if (recebimento == null) return NotFound();
        if (recebimento.Status == "finalizado")
            return BadRequest(new { erro = "Recebimento ja finalizado." });
        if (recebimento.Status == "pendente")
            return BadRequest(new { erro = "Confirme as quantidades recebidas antes de finalizar." });

        // Atualiza estoque para cada item com produto vinculado
        foreach (var item in recebimento.Itens.Where(i => i.ProdutoId.HasValue && i.QuantidadeRecebida > 0))
        {
            var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == item.ProdutoId!.Value);
            if (produto == null) continue;

            produto.EstoqueAtual += item.QuantidadeRecebida;

            _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                Id          = Guid.NewGuid(),
                TenantId    = _t.TenantId,
                ProdutoId   = produto.Id,
                Tipo        = "entrada",
                Quantidade  = item.QuantidadeRecebida,
                EstoqueApos = produto.EstoqueAtual,
                Motivo      = $"Recebimento #{recebimento.Id.ToString()[..8].ToUpper()} - Pedido #{recebimento.Pedido?.Numero}"
            });
        }

        recebimento.Status          = "finalizado";
        recebimento.DataRecebimento = DateOnly.FromDateTime(DateTime.Today);

        // Atualiza status do pedido para recebido
        if (recebimento.PedidoId.HasValue)
        {
            var pedido = await _db.PedidosCompra.FirstOrDefaultAsync(p => p.Id == recebimento.PedidoId.Value);
            if (pedido != null)
            {
                pedido.Status       = "recebido";
                pedido.AtualizadoEm = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Estoque atualizado com sucesso." });
    }

    // ── Anexos (NF, DANFE, boleto, etc.) ─────────────────────────────────────

    [HttpGet("{id}/anexos")]
    public async Task<IActionResult> ListarAnexos(Guid id)
    {
        var recebimento = await _db.RecebimentosMercadoria
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _t.TenantId);
        if (recebimento == null) return NotFound();

        var anexos = await _db.RecebimentoAnexos
            .Where(a => a.RecebimentoId == id && a.TenantId == _t.TenantId)
            .OrderBy(a => a.CriadoEm)
            .Select(a => new {
                a.Id, a.Nome, a.TipoArquivo, a.TamanhoBytes,
                a.Descricao, a.CriadoEm
                // DadosBase64 omitido na listagem — só no download
            })
            .ToListAsync();

        return Ok(anexos);
    }

    [HttpGet("anexos/{anexoId}/download")]
    public async Task<IActionResult> Download(Guid anexoId)
    {
        var anexo = await _db.RecebimentoAnexos
            .FirstOrDefaultAsync(a => a.Id == anexoId && a.TenantId == _t.TenantId);
        if (anexo == null) return NotFound();

        var bytes = Convert.FromBase64String(anexo.DadosBase64);
        return File(bytes, anexo.TipoArquivo, anexo.Nome);
    }

    public record AnexoUploadRequest(
        string Nome,
        string TipoArquivo,
        string DadosBase64,
        string? Descricao
    );

    [HttpPost("{id}/anexos")]
    public async Task<IActionResult> UploadAnexo(Guid id, AnexoUploadRequest req)
    {
        var recebimento = await _db.RecebimentosMercadoria
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _t.TenantId);
        if (recebimento == null) return NotFound();

        // Valida tamanho (max 5MB em base64 ~= 7MB de texto)
        if (req.DadosBase64.Length > 7 * 1024 * 1024)
            return BadRequest(new { erro = "Arquivo muito grande. Maximo 5MB." });

        // Valida tipo permitido
        var tiposPermitidos = new[] {
            "application/pdf", "image/jpeg", "image/jpg", "image/png",
            "image/webp", "application/xml", "text/xml"
        };
        if (!tiposPermitidos.Contains(req.TipoArquivo.ToLower()))
            return BadRequest(new { erro = "Tipo de arquivo nao permitido. Use PDF, JPG, PNG ou XML." });

        var tamanho = Convert.FromBase64String(req.DadosBase64).Length;

        var anexo = new VetClinica.API.Models.RecebimentoAnexo
        {
            Id             = Guid.NewGuid(),
            TenantId       = _t.TenantId,
            RecebimentoId  = id,
            Nome           = req.Nome,
            TipoArquivo    = req.TipoArquivo.ToLower(),
            TamanhoBytes   = tamanho,
            DadosBase64    = req.DadosBase64,
            Descricao      = req.Descricao,
            CriadoPor      = _t.UserId,
            CriadoEm      = DateTime.UtcNow
        };

        _db.RecebimentoAnexos.Add(anexo);
        await _db.SaveChangesAsync();

        return Ok(new {
            anexo.Id, anexo.Nome, anexo.TipoArquivo,
            anexo.TamanhoBytes, anexo.Descricao
        });
    }

    [HttpDelete("anexos/{anexoId}")]
    public async Task<IActionResult> RemoverAnexo(Guid anexoId)
    {
        var anexo = await _db.RecebimentoAnexos
            .FirstOrDefaultAsync(a => a.Id == anexoId && a.TenantId == _t.TenantId);
        if (anexo == null) return NotFound();

        _db.RecebimentoAnexos.Remove(anexo);
        await _db.SaveChangesAsync();
        return NoContent();
    }

}
