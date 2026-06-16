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
[Route("api/financeiro")]
public class FinanceiroController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;

    public FinanceiroController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    // ================================================================
    //  CATEGORIAS
    // ================================================================

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias([FromQuery] string? tipo)
    {
        var query = _db.CategoriasFinanceiras
            .Where(c => c.TenantId == _t.TenantId && c.Ativo);

        if (!string.IsNullOrEmpty(tipo))
            query = query.Where(c => c.Tipo == tipo);

        var items = await query
            .OrderBy(c => c.Tipo).ThenBy(c => c.Nome)
            .Select(c => new CategoriaFinanceiraDto(c.Id, c.Nome, c.Tipo, c.Ativo))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("categorias")]
    public async Task<IActionResult> CriarCategoria(CategoriaCreate dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest(new { erro = "Nome obrigatório" });
        if (dto.Tipo != "receita" && dto.Tipo != "despesa")
            return BadRequest(new { erro = "Tipo deve ser 'receita' ou 'despesa'" });

        var cat = new CategoriaFinanceira
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Nome = dto.Nome.Trim(),
            Tipo = dto.Tipo,
            Ativo = true
        };
        _db.CategoriasFinanceiras.Add(cat);
        await _db.SaveChangesAsync();
        return Ok(new CategoriaFinanceiraDto(cat.Id, cat.Nome, cat.Tipo, cat.Ativo));
    }

    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> RemoverCategoria(Guid id)
    {
        var cat = await _db.CategoriasFinanceiras
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (cat == null) return NotFound();

        var emUso = await _db.Contas.AnyAsync(c => c.CategoriaId == id);
        if (emUso)
        {
            cat.Ativo = false;
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Categoria desativada (em uso por contas existentes)" });
        }

        _db.CategoriasFinanceiras.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ================================================================
    //  CONTAS A PAGAR / RECEBER
    // ================================================================

    [HttpGet("contas")]
    public async Task<IActionResult> ListarContas(
        [FromQuery] string? tipo,
        [FromQuery] string? status,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] bool? vencidas)
    {
        var query = _db.Contas
            .Include(c => c.Categoria)
            .Where(c => c.TenantId == _t.TenantId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(tipo))
            query = query.Where(c => c.Tipo == tipo);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (vencidas == true)
            query = query.Where(c => c.Status == "aberta" && c.DataVencimento < DateOnly.FromDateTime(DateTime.Today));

        if (de.HasValue)
            query = query.Where(c => c.DataVencimento >= de.Value);

        if (ate.HasValue)
            query = query.Where(c => c.DataVencimento <= ate.Value);

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var items = await query
            .OrderBy(c => c.DataVencimento)
            .Select(c => new ContaDto(
                c.Id,
                c.Tipo,
                c.Descricao,
                c.Valor,
                c.DataCompetencia,
                c.DataVencimento,
                c.FormaPagamento,
                c.Status,
                c.ValorPago,
                c.DataBaixa,
                c.ContaBancaria,
                c.OsId,
                c.VendaId,
                c.Categoria != null ? c.Categoria.Nome : null,
                c.Status == "aberta" && c.DataVencimento < hoje
                    ? (int)(hoje.DayNumber - c.DataVencimento.DayNumber)
                    : (int?)null
            ))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("contas/{id:guid}")]
    public async Task<IActionResult> ObterConta(Guid id)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var c = await _db.Contas
            .Include(c => c.Categoria)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);

        if (c == null) return NotFound();

        return Ok(new ContaDto(
            c.Id, c.Tipo, c.Descricao, c.Valor, c.DataCompetencia, c.DataVencimento,
            c.FormaPagamento, c.Status, c.ValorPago, c.DataBaixa, c.ContaBancaria,
            c.OsId, c.VendaId,
            c.Categoria?.Nome,
            c.Status == "aberta" && c.DataVencimento < hoje
                ? (int)(hoje.DayNumber - c.DataVencimento.DayNumber)
                : (int?)null
        ));
    }

    [HttpPost("contas")]
    public async Task<IActionResult> CriarConta(ContaCreate dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descricao))
            return BadRequest(new { erro = "Descrição obrigatória" });
        if (dto.Valor <= 0)
            return BadRequest(new { erro = "Valor deve ser positivo" });
        if (dto.Tipo != "receita" && dto.Tipo != "despesa")
            return BadRequest(new { erro = "Tipo inválido" });

        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Tipo = dto.Tipo,
            Descricao = dto.Descricao.Trim(),
            Valor = dto.Valor,
            DataCompetencia = dto.DataCompetencia,
            DataVencimento = dto.DataVencimento,
            FormaPagamento = dto.FormaPagamento,
            CategoriaId = dto.CategoriaId,
            ContaBancaria = dto.ContaBancaria,
            OsId = dto.OsId,
            VendaId = dto.VendaId,
            Status = "aberta",
            CriadoPor = _t.UserId,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        _db.Contas.Add(conta);
        await _db.SaveChangesAsync();
        return Ok(new { conta.Id });
    }

    [HttpPut("contas/{id:guid}")]
    public async Task<IActionResult> EditarConta(Guid id, ContaCreate dto)
    {
        var conta = await _db.Contas
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (conta == null) return NotFound();
        if (conta.Status != "aberta")
            return BadRequest(new { erro = "Só contas abertas podem ser editadas" });

        conta.Descricao = dto.Descricao.Trim();
        conta.Valor = dto.Valor;
        conta.DataCompetencia = dto.DataCompetencia;
        conta.DataVencimento = dto.DataVencimento;
        conta.FormaPagamento = dto.FormaPagamento;
        conta.CategoriaId = dto.CategoriaId;
        conta.ContaBancaria = dto.ContaBancaria;
        conta.AtualizadoEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("contas/{id:guid}")]
    public async Task<IActionResult> CancelarConta(Guid id)
    {
        var conta = await _db.Contas
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (conta == null) return NotFound();
        if (conta.Status is "paga" or "recebida")
            return BadRequest(new { erro = "Conta já baixada não pode ser cancelada" });

        conta.Status = "cancelada";
        conta.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ================================================================
    //  BAIXA (pagamento / recebimento)
    //  FIX: agora cria MovimentacaoBancaria quando ContaBancariaId é informado
    // ================================================================

    [HttpPost("contas/{id:guid}/baixar")]
    public async Task<IActionResult> BaixarConta(Guid id, BaixaContaRequest dto)
    {
        if (dto.ValorPago <= 0)
            return BadRequest(new { erro = "Valor pago deve ser positivo" });

        var conta = await _db.Contas
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (conta == null) return NotFound();
        if (conta.Status != "aberta")
            return BadRequest(new { erro = $"Conta já está com status '{conta.Status}'" });

        // Registra baixa
        conta.ValorPago       = dto.ValorPago;
        conta.DataBaixa       = dto.DataBaixa;
        conta.FormaPagamento  = dto.FormaPagamento ?? conta.FormaPagamento;
        conta.ContaBancaria   = dto.ContaBancariaNome ?? conta.ContaBancaria;
        conta.ObsBaixa        = dto.ObsBaixa;
        conta.Status          = conta.Tipo == "receita" ? "recebida" : "paga";
        conta.AtualizadoEm    = DateTime.UtcNow;

        // Gera lançamento contábil (histórico)
        var lanc = new Lancamento
        {
            Id          = Guid.NewGuid(),
            TenantId    = _t.TenantId,
            OsId        = conta.OsId,
            CategoriaId = conta.CategoriaId,
            Data        = dto.DataBaixa,
            Tipo        = conta.Tipo,
            Valor       = dto.ValorPago,
            Descricao   = $"Baixa: {conta.Descricao}",
            CriadoEm   = DateTime.UtcNow
        };
        _db.Lancamentos.Add(lanc);

        // ── FIX: cria MovimentacaoBancaria para atualizar saldo ──────────────
        if (dto.ContaBancariaId.HasValue)
        {
            var contaBancariaExiste = await _db.ContasBancarias
                .AnyAsync(cb => cb.Id == dto.ContaBancariaId.Value && cb.TenantId == _t.TenantId);

            if (contaBancariaExiste)
            {
                // receita = entrada na conta bancária; despesa = saída
                var tipoMov = conta.Tipo == "receita" ? "entrada" : "saida";

                var mov = new MovimentacaoBancaria
                {
                    Id               = Guid.NewGuid(),
                    TenantId         = _t.TenantId,
                    ContaBancariaId  = dto.ContaBancariaId.Value,
                    Tipo             = tipoMov,
                    Valor            = dto.ValorPago,
                    Descricao        = $"Baixa financeiro: {conta.Descricao}",
                    DataMovimentacao = dto.DataBaixa,
                    CategoriaId      = conta.CategoriaId,
                    ContaId          = conta.Id,
                    Conciliado       = false,
                    Origem           = "baixa_financeiro",
                    CriadoPor        = _t.UserId,
                    CriadoEm        = DateTime.UtcNow
                };
                _db.MovimentacoesBancarias.Add(mov);
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Baixa registrada com sucesso", lancamentoId = lanc.Id });
    }

    [HttpPost("contas/{id:guid}/estornar")]
    public async Task<IActionResult> EstornarBaixa(Guid id)
    {
        var conta = await _db.Contas
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (conta == null) return NotFound();
        if (conta.Status is not ("paga" or "recebida"))
            return BadRequest(new { erro = "Somente contas baixadas podem ser estornadas" });

        conta.Status      = "aberta";
        conta.ValorPago   = null;
        conta.DataBaixa   = null;
        conta.ObsBaixa    = null;
        conta.AtualizadoEm = DateTime.UtcNow;

        // Remove movimentacao bancaria gerada pela baixa, se existir e não conciliada
        var movBaixa = await _db.MovimentacoesBancarias
            .FirstOrDefaultAsync(m => m.ContaId == id
                                   && m.Origem == "baixa_financeiro"
                                   && m.TenantId == _t.TenantId
                                   && !m.Conciliado);
        if (movBaixa != null)
            _db.MovimentacoesBancarias.Remove(movBaixa);

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Estorno realizado" });
    }

    // ================================================================
    //  DASHBOARD / RESUMO
    // ================================================================

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
    {
        var inicio = de ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var fim = ate ?? DateOnly.FromDateTime(DateTime.Today);
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        var contas = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId)
            .ToListAsync();

        var baixadas = contas.Where(c =>
            c.DataBaixa.HasValue && c.DataBaixa >= inicio && c.DataBaixa <= fim &&
            c.Status is "paga" or "recebida").ToList();

        var receitas = baixadas.Where(c => c.Tipo == "receita").Sum(c => c.ValorPago ?? 0);
        var despesas = baixadas.Where(c => c.Tipo == "despesa").Sum(c => c.ValorPago ?? 0);

        var abertas = contas.Where(c => c.Status == "aberta").ToList();
        var receber = abertas.Where(c => c.Tipo == "receita").Sum(c => c.Valor);
        var pagar   = abertas.Where(c => c.Tipo == "despesa").Sum(c => c.Valor);

        var vencidas = abertas.Where(c => c.DataVencimento < hoje).ToList();

        return Ok(new ResumoFinanceiroM2(
            receitas, despesas, receitas - despesas,
            receber, pagar,
            vencidas.Count,
            vencidas.Sum(c => c.Valor)
        ));
    }

    [HttpGet("fluxo")]
    public async Task<IActionResult> FluxoCaixa([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
    {
        var inicio = de ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var fim = ate ?? DateOnly.FromDateTime(DateTime.Today);

        var baixadas = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId &&
                        c.DataBaixa.HasValue &&
                        c.DataBaixa >= inicio &&
                        c.DataBaixa <= fim &&
                        (c.Status == "paga" || c.Status == "recebida"))
            .ToListAsync();

        var fluxo = baixadas
            .GroupBy(c => c.DataBaixa!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new FluxoDia(
                g.Key,
                g.Where(c => c.Tipo == "receita").Sum(c => c.ValorPago ?? 0),
                g.Where(c => c.Tipo == "despesa").Sum(c => c.ValorPago ?? 0)
            ))
            .ToList();

        return Ok(fluxo);
    }

    // ================================================================
    //  LANÇAMENTOS (histórico — mantém compatibilidade)
    // ================================================================

    [HttpGet("lancamentos")]
    public async Task<IActionResult> Lancamentos([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
    {
        var inicio = de ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var fim = ate ?? DateOnly.FromDateTime(DateTime.Today);

        var items = await _db.Lancamentos
            .Where(l => l.TenantId == _t.TenantId && l.Data >= inicio && l.Data <= fim)
            .OrderByDescending(l => l.Data)
            .Select(l => new LancamentoDto(l.Id, l.Data, l.Tipo, l.Valor, l.Descricao))
            .ToListAsync();

        return Ok(items);
    }
}
