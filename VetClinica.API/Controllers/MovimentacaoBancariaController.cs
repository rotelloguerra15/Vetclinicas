using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/bancario")]
public class MovimentacaoBancariaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public MovimentacaoBancariaController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    // ── Contas bancárias ──────────────────────────────────────────────────────

    [HttpGet("contas")]
    public async Task<IActionResult> ListarContas()
    {
        var contas = await _db.ContasBancarias
            .Where(c => c.TenantId == _t.TenantId && c.Ativo)
            .OrderBy(c => c.Nome)
            .ToListAsync();

        // Calcula saldo atual para cada conta
        var result = new List<object>();
        foreach (var conta in contas)
        {
            var entradas = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == conta.Id && m.Tipo == "entrada")
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var saidas = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == conta.Id && m.Tipo == "saida")
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var transferenciasSaida = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == conta.Id && m.Tipo == "transferencia")
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var transferenciasEntrada = await _db.MovimentacoesBancarias
                .Where(m => m.ContaDestinoId == conta.Id && m.Tipo == "transferencia")
                .SumAsync(m => (decimal?)m.Valor) ?? 0;

            var saldoAtual = conta.SaldoInicial + entradas - saidas - transferenciasSaida + transferenciasEntrada;
            result.Add(new {
                conta.Id, conta.Nome, conta.Banco, conta.Agencia,
                conta.Conta, conta.Tipo, conta.SaldoInicial,
                SaldoAtual = saldoAtual
            });
        }
        return Ok(result);
    }

    public record ContaBancariaCreate(
        string Nome, string? Banco, string? Agencia,
        string? Conta, string Tipo, decimal SaldoInicial);

    [HttpPost("contas")]
    public async Task<IActionResult> CriarConta(ContaBancariaCreate dto)
    {
        var c = new ContaBancaria
        {
            Id           = Guid.NewGuid(),
            TenantId     = _t.TenantId,
            Nome         = dto.Nome,
            Banco        = dto.Banco,
            Agencia      = dto.Agencia,
            Conta        = dto.Conta,
            Tipo         = dto.Tipo,
            SaldoInicial = dto.SaldoInicial,
            CriadoEm     = DateTime.UtcNow
        };
        _db.ContasBancarias.Add(c);
        await _db.SaveChangesAsync();
        return Ok(new { c.Id });
    }

    [HttpPut("contas/{id}")]
    public async Task<IActionResult> EditarConta(Guid id, ContaBancariaCreate dto)
    {
        var c = await _db.ContasBancarias
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (c == null) return NotFound();
        c.Nome = dto.Nome; c.Banco = dto.Banco; c.Agencia = dto.Agencia;
        c.Conta = dto.Conta; c.Tipo = dto.Tipo; c.SaldoInicial = dto.SaldoInicial;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("contas/{id}")]
    public async Task<IActionResult> RemoverConta(Guid id)
    {
        var c = await _db.ContasBancarias
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (c == null) return NotFound();
        var temMovs = await _db.MovimentacoesBancarias.AnyAsync(m => m.ContaBancariaId == id);
        if (temMovs) return BadRequest(new { erro = "Conta tem movimentacoes e nao pode ser removida." });
        c.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Movimentações ─────────────────────────────────────────────────────────

    [HttpGet("movimentacoes")]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? contaId,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] string? tipo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        var inicio = de ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var fim    = ate ?? DateOnly.FromDateTime(DateTime.Today);

        var q = _db.MovimentacoesBancarias
            .Include(m => m.ContaBancaria)
            .Include(m => m.Categoria)
            .Where(m => m.TenantId == _t.TenantId
                     && m.DataMovimentacao >= inicio
                     && m.DataMovimentacao <= fim);

        if (contaId.HasValue) q = q.Where(m => m.ContaBancariaId == contaId.Value);
        if (!string.IsNullOrEmpty(tipo)) q = q.Where(m => m.Tipo == tipo);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(m => m.DataMovimentacao)
            .ThenByDescending(m => m.CriadoEm)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new {
                m.Id, m.Tipo, m.Valor, m.Descricao, m.DataMovimentacao,
                m.Conciliado,
                ContaNome = m.ContaBancaria!.Nome,
                CategoriaNome = m.Categoria != null ? m.Categoria.Nome : null,
                m.ContaId
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    public record MovimentacaoCreate(
        Guid ContaBancariaId,
        string Tipo,
        decimal Valor,
        string Descricao,
        DateOnly DataMovimentacao,
        Guid? CategoriaId,
        Guid? ContaDestinoId);

    [HttpPost("movimentacoes")]
    public async Task<IActionResult> Criar(MovimentacaoCreate dto)
    {
        var m = new MovimentacaoBancaria
        {
            Id               = Guid.NewGuid(),
            TenantId         = _t.TenantId,
            ContaBancariaId  = dto.ContaBancariaId,
            Tipo             = dto.Tipo,
            Valor            = dto.Valor,
            Descricao        = dto.Descricao,
            DataMovimentacao = dto.DataMovimentacao,
            CategoriaId      = dto.CategoriaId,
            ContaDestinoId   = dto.ContaDestinoId,
            CriadoPor        = _t.UserId,
            CriadoEm         = DateTime.UtcNow
        };
        _db.MovimentacoesBancarias.Add(m);
        await _db.SaveChangesAsync();
        return Ok(new { m.Id });
    }

    [HttpDelete("movimentacoes/{id}")]
    public async Task<IActionResult> Remover(Guid id)
    {
        var m = await _db.MovimentacoesBancarias
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (m == null) return NotFound();
        if (m.Conciliado)
            return BadRequest(new { erro = "Movimentacao conciliada nao pode ser removida." });
        _db.MovimentacoesBancarias.Remove(m);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("movimentacoes/{id}/conciliar")]
    public async Task<IActionResult> Conciliar(Guid id)
    {
        var m = await _db.MovimentacoesBancarias
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (m == null) return NotFound();
        m.Conciliado = !m.Conciliado;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Histórico por cliente ─────────────────────────────────────────────────

    [HttpGet("historico-cliente/{tutorId}")]
    public async Task<IActionResult> HistoricoCliente(Guid tutorId,
        [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
    {
        var inicio = de ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(-12));
        var fim    = ate ?? DateOnly.FromDateTime(DateTime.Today);

        var tutor = await _db.Tutores
            .FirstOrDefaultAsync(t => t.Id == tutorId && t.TenantId == _t.TenantId);
        if (tutor == null) return NotFound();

        // Contas pagas/recebidas vinculadas a OS ou PDV do tutor
        var contas = await _db.Contas
            .Include(c => c.Categoria)
            .Where(c => c.TenantId == _t.TenantId
                     && c.Status != "cancelada"
                     && c.DataCompetencia >= inicio
                     && c.DataCompetencia <= fim
                     && (
                         _db.OrdemServico.Any(os => os.Id == c.OsId && os.PetId != null &&
                             _db.Pets.Any(p => p.Id == os.PetId && p.TutorId == tutorId))
                         ||
                         _db.Vendas.Any(v => v.Id == c.VendaId &&
                             _db.Pets.Any(p => p.Id == v.PetId && p.TutorId == tutorId))
                     ))
            .OrderByDescending(c => c.DataCompetencia)
            .Select(c => new {
                c.Id, c.Tipo, c.Descricao, c.Valor, c.ValorPago,
                c.Status, c.DataCompetencia, c.DataVencimento, c.DataBaixa,
                c.FormaPagamento,
                CategoriaNome = c.Categoria != null ? c.Categoria.Nome : null
            })
            .ToListAsync();

        var totalGasto    = contas.Where(c => c.Tipo == "receita" && c.Status != "aberta").Sum(c => c.ValorPago ?? 0);
        var totalAberto   = contas.Where(c => c.Status == "aberta").Sum(c => c.Valor);
        var totalServicos = contas.Count(c => c.Tipo == "receita");

        return Ok(new {
            tutor = new { tutor.Id, tutor.Nome, tutor.Telefone, tutor.Email },
            resumo = new { totalGasto, totalAberto, totalServicos },
            contas
        });
    }

    // ── Histórico geral (lista tutores com total pago) ────────────────────────

    [HttpGet("historico-clientes")]
    public async Task<IActionResult> HistoricoClientes(
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] string? busca,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var inicio = de ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(-3));
        var fim    = ate ?? DateOnly.FromDateTime(DateTime.Today);

        var query = _db.Tutores
            .Where(t => t.TenantId == _t.TenantId && t.Ativo);

        if (!string.IsNullOrEmpty(busca))
            query = query.Where(t => t.Nome.ToLower().Contains(busca.ToLower()));

        var total  = await query.CountAsync();
        var tutores = await query.OrderBy(t => t.Nome)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new {
                t.Id, t.Nome, t.Telefone,
                QtdPets = t.Pets.Count(p => p.Ativo)
            })
            .ToListAsync();

        // Para cada tutor, busca total pago no periodo
        var result = new List<object>();
        foreach (var t in tutores)
        {
            var totalPago = await _db.Contas
                .Where(c => c.TenantId == _t.TenantId
                         && c.Tipo == "receita"
                         && c.Status != "cancelada"
                         && c.DataCompetencia >= inicio
                         && c.DataCompetencia <= fim
                         && (
                             _db.OrdemServico.Any(os => os.Id == c.OsId && os.PetId != null &&
                                 _db.Pets.Any(p => p.Id == os.PetId && p.TutorId == t.Id))
                             ||
                             _db.Vendas.Any(v => v.Id == c.VendaId &&
                                 _db.Pets.Any(p => p.Id == v.PetId && p.TutorId == t.Id))
                         ))
                .SumAsync(c => (decimal?)(c.ValorPago ?? 0)) ?? 0;

            result.Add(new { t.Id, t.Nome, t.Telefone, t.QtdPets, TotalPago = totalPago });
        }

        return Ok(new { items = result, total, page, pageSize });
    }
}
