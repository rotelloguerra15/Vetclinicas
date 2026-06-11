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
[Route("api/vendas")]
public class VendasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public VendasController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateTime? de, [FromQuery] DateTime? ate)
    {
        var inicio = de ?? DateTime.Today.AddDays(-30);
        var fim = ate ?? DateTime.Today.AddDays(1);

        var query = from v in _db.Vendas
                    where v.TenantId == _t.TenantId && v.CriadoEm >= inicio && v.CriadoEm < fim
                    join t in _db.Tutores on v.TutorId equals t.Id into gt
                    from t in gt.DefaultIfEmpty()
                    orderby v.CriadoEm descending
                    select new VendaDto(v.Id, v.TutorId, t != null ? t.Nome : null,
                        v.Status, v.ValorTotal, v.FormaPagamento, v.CriadoEm);

        return Ok(await query.ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Criar(VendaCreate dto)
    {
        if (dto.Itens == null || dto.Itens.Count == 0)
            return BadRequest(new { erro = "Venda sem itens" });

        // Valida estoque
        foreach (var item in dto.Itens)
        {
            var prod = await _db.Produtos
                .FirstOrDefaultAsync(p => p.Id == item.ProdutoId && p.TenantId == _t.TenantId);
            if (prod == null)
                return BadRequest(new { erro = $"Produto {item.ProdutoId} não encontrado" });
            if (prod.EstoqueAtual < item.Quantidade)
                return BadRequest(new { erro = $"Estoque insuficiente de {prod.Nome}" });
        }

        var valorProdutos = dto.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        var total = valorProdutos - dto.Desconto;

        // 1. Salva a venda
        var venda = new Venda
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            TutorId = dto.TutorId,
            UserId = _t.UserId,
            Status = "aberta",
            ValorProdutos = valorProdutos,
            ValorDesconto = dto.Desconto,
            ValorTotal = total,
            FormaPagamento = dto.FormaPagamento,
            CriadoEm = DateTime.UtcNow
        };
        _db.Vendas.Add(venda);
        await _db.SaveChangesAsync();

        // 2. Itens (FK já existe)
        foreach (var item in dto.Itens)
        {
            _db.VendaItens.Add(new VendaItem
            {
                Id = Guid.NewGuid(),
                VendaId = venda.Id,
                ProdutoId = item.ProdutoId,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                Subtotal = item.Quantidade * item.PrecoUnitario
            });
        }
        await _db.SaveChangesAsync();

        // 3. Finaliza a venda
        venda.Status = "finalizada";
        venda.FinalizadaEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 4. Marca a OS como paga (some da fila do PDV)
        if (dto.OsId.HasValue)
        {
            var os = await _db.OrdensServico
                .FirstOrDefaultAsync(o => o.Id == dto.OsId && o.TenantId == _t.TenantId);
            if (os != null)
            {
                os.Status = "pago";
                await _db.SaveChangesAsync();
            }
        }

        // ── Vínculo financeiro: Venda PDV → Conta a Receber ──────────
        if (venda.ValorTotal > 0)
        {
            var nomeTutor = dto.TutorId.HasValue
                ? await _db.Tutores
                    .Where(t => t.Id == dto.TutorId)
                    .Select(t => t.Nome)
                    .FirstOrDefaultAsync()
                : null;

            var descricao = nomeTutor != null
                ? $"PDV — {nomeTutor} ({venda.Id.ToString()[..8].ToUpper()})"
                : $"PDV — Venda ({venda.Id.ToString()[..8].ToUpper()})";

            // Tenta encontrar categoria "Vendas PDV" ou primeira receita disponível
            var categoriaId = await _db.CategoriasFinanceiras
                .Where(c => c.TenantId == _t.TenantId && c.Tipo == "receita" && c.Ativo)
                .OrderBy(c => c.Nome.Contains("PDV") || c.Nome.Contains("Venda") ? 0 : 1)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync();

            // A conta já nasce com status "recebida" se a forma de pagamento for imediata
            var formasImediatas = new[] { "Dinheiro", "PIX", "Cartão de Débito", "Cartão de Crédito" };
            var pagamentoImediato = formasImediatas.Any(f =>
                string.Equals(f, dto.FormaPagamento, StringComparison.OrdinalIgnoreCase));

            var conta = new Conta
            {
                Id = Guid.NewGuid(),
                TenantId = _t.TenantId,
                VendaId = venda.Id,
                Tipo = "receita",
                Descricao = descricao,
                Valor = venda.ValorTotal,
                DataCompetencia = DateOnly.FromDateTime(DateTime.Today),
                DataVencimento = DateOnly.FromDateTime(DateTime.Today),
                FormaPagamento = dto.FormaPagamento,
                CategoriaId = categoriaId,
                Status = pagamentoImediato ? "recebida" : "aberta",
                ValorPago = pagamentoImediato ? venda.ValorTotal : null,
                DataBaixa = pagamentoImediato ? DateOnly.FromDateTime(DateTime.Today) : null,
                ContaBancaria = "Caixa",
                CriadoPor = _t.UserId,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };

            _db.Contas.Add(conta);

            // Se já recebida, gera lançamento contábil + MovimentacaoBancaria
            if (pagamentoImediato)
            {
                _db.Lancamentos.Add(new Lancamento
                {
                    Id = Guid.NewGuid(),
                    TenantId = _t.TenantId,
                    OsId = null,
                    CategoriaId = categoriaId,
                    Data = DateOnly.FromDateTime(DateTime.Today),
                    Tipo = "receita",
                    Valor = venda.ValorTotal,
                    Descricao = $"Baixa automatica: {descricao}",
                    CriadoEm = DateTime.UtcNow
                });

                // ── MovimentacaoBancaria: atualiza saldo da conta bancária ──
                var contaBancaria = await _db.ContasBancarias
                    .Where(c => c.TenantId == _t.TenantId && c.Ativo)
                    .OrderBy(c => c.CriadoEm)
                    .FirstOrDefaultAsync();

                if (contaBancaria != null)
                {
                    await _db.SaveChangesAsync(); // salva a Conta primeiro para ter o Id
                    _db.MovimentacoesBancarias.Add(new MovimentacaoBancaria
                    {
                        Id               = Guid.NewGuid(),
                        TenantId         = _t.TenantId,
                        ContaBancariaId  = contaBancaria.Id,
                        Tipo             = "entrada",
                        Valor            = venda.ValorTotal,
                        Descricao        = descricao,
                        DataMovimentacao = DateOnly.FromDateTime(DateTime.Today),
                        CategoriaId      = categoriaId,
                        ContaId          = conta.Id,
                        Origem           = "caixa",
                        Conciliado       = false,
                        CriadoPor        = _t.UserId,
                        CriadoEm        = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        return Ok(new { venda.Id, venda.ValorTotal });
    }
}
