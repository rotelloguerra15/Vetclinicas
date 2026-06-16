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
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public MovimentacaoBancariaController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    // ── Contas bancárias ──────────────────────────────────────────────────────

    [HttpGet("contas")]
    public async Task<IActionResult> ListarContas()
    {
        var contas = await _db.ContasBancarias
            .Where(c => c.TenantId == _t.TenantId && c.Ativo)
            .OrderBy(c => c.Nome)
            .ToListAsync();

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
        return Ok(new { c.Id, c.Nome });
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
                m.Conciliado, m.Origem,
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
            Origem           = "manual",
            CriadoPor        = _t.UserId,
            CriadoEm        = DateTime.UtcNow
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

    // ── Conciliação Diária ────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/bancario/conciliacao/{contaId}/{data}
    /// Retorna o resumo do dia: saldo anterior, movimentações, saldo calculado,
    /// e se já existe um fechamento para esse dia.
    /// </summary>
    [HttpGet("conciliacao/{contaId}/{data}")]
    public async Task<IActionResult> ObterDia(Guid contaId, DateOnly data)
    {
        var conta = await _db.ContasBancarias
            .FirstOrDefaultAsync(c => c.Id == contaId && c.TenantId == _t.TenantId && c.Ativo);
        if (conta == null) return NotFound(new { erro = "Conta não encontrada." });

        // Saldo anterior: busca o fechamento do dia imediatamente anterior
        // Se não houver fechamento anterior, usa saldo_inicial da conta
        var fechamentoAnterior = await _db.ConciliacoesDiarias
            .Where(c => c.ContaBancariaId == contaId
                     && c.TenantId == _t.TenantId
                     && c.DataConciliacao < data)
            .OrderByDescending(c => c.DataConciliacao)
            .FirstOrDefaultAsync();

        decimal saldoAnterior;
        if (fechamentoAnterior != null)
            saldoAnterior = fechamentoAnterior.SaldoFinal;
        else
        {
            // Calcula desde o início até o dia anterior
            var entradasAnt = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == contaId && m.TenantId == _t.TenantId
                         && m.Tipo == "entrada" && m.DataMovimentacao < data)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var saidasAnt = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == contaId && m.TenantId == _t.TenantId
                         && m.Tipo == "saida" && m.DataMovimentacao < data)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var transEntAnt = await _db.MovimentacoesBancarias
                .Where(m => m.ContaDestinoId == contaId && m.TenantId == _t.TenantId
                         && m.Tipo == "transferencia" && m.DataMovimentacao < data)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var transSaiAnt = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == contaId && m.TenantId == _t.TenantId
                         && m.Tipo == "transferencia" && m.DataMovimentacao < data)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;

            saldoAnterior = conta.SaldoInicial + entradasAnt - saidasAnt + transEntAnt - transSaiAnt;
        }

        // Movimentações do dia
        var movsDia = await _db.MovimentacoesBancarias
            .Include(m => m.Categoria)
            .Where(m => m.ContaBancariaId == contaId
                     && m.TenantId == _t.TenantId
                     && m.DataMovimentacao == data)
            .OrderBy(m => m.CriadoEm)
            .Select(m => new {
                m.Id, m.Tipo, m.Valor, m.Descricao, m.Conciliado, m.Origem,
                CategoriaNome = m.Categoria != null ? m.Categoria.Nome : null
            })
            .ToListAsync();

        var totalEntradas    = movsDia.Where(m => m.Tipo == "entrada").Sum(m => m.Valor);
        var totalSaidas      = movsDia.Where(m => m.Tipo == "saida").Sum(m => m.Valor);
        var totalTransfSaida = movsDia.Where(m => m.Tipo == "transferencia").Sum(m => m.Valor);
        // Transferências recebidas de outras contas no mesmo dia
        var totalTransfEntrada = await _db.MovimentacoesBancarias
            .Where(m => m.ContaDestinoId == contaId
                     && m.TenantId == _t.TenantId
                     && m.DataMovimentacao == data
                     && m.Tipo == "transferencia")
            .SumAsync(m => (decimal?)m.Valor) ?? 0;

        var saldoCalculado = saldoAnterior + totalEntradas - totalSaidas
                           + totalTransfEntrada - totalTransfSaida;

        // Verifica se já foi fechado
        var fechamento = await _db.ConciliacoesDiarias
            .FirstOrDefaultAsync(c => c.ContaBancariaId == contaId
                                   && c.TenantId == _t.TenantId
                                   && c.DataConciliacao == data);

        return Ok(new {
            conta = new { conta.Id, conta.Nome, conta.Banco },
            data,
            saldoAnterior,
            movimentacoes = movsDia,
            totalEntradas,
            totalSaidas,
            totalTransferencias = totalTransfEntrada - totalTransfSaida,
            saldoCalculado,
            fechamento = fechamento == null ? null : new {
                fechamento.Id,
                fechamento.SaldoExtrato,
                fechamento.Diferenca,
                fechamento.Observacao,
                fechamento.FechadoEm
            }
        });
    }

    public record FecharDiaRequest(
        Guid ContaBancariaId,
        DateOnly DataConciliacao,
        decimal? SaldoExtrato,    // saldo informado pelo extrato do banco (opcional)
        string? Observacao);

    /// <summary>
    /// POST /api/bancario/conciliacao/fechar
    /// Fecha o dia: calcula saldo, registra conciliação, marca movimentações do dia como conciliadas.
    /// </summary>
    [HttpPost("conciliacao/fechar")]
    public async Task<IActionResult> FecharDia(FecharDiaRequest dto)
    {
        var conta = await _db.ContasBancarias
            .FirstOrDefaultAsync(c => c.Id == dto.ContaBancariaId
                                   && c.TenantId == _t.TenantId && c.Ativo);
        if (conta == null) return NotFound(new { erro = "Conta não encontrada." });

        // Impede duplo fechamento
        var jaFechado = await _db.ConciliacoesDiarias
            .AnyAsync(c => c.ContaBancariaId == dto.ContaBancariaId
                        && c.TenantId == _t.TenantId
                        && c.DataConciliacao == dto.DataConciliacao);
        if (jaFechado)
            return BadRequest(new { erro = "Este dia já foi fechado para esta conta." });

        // Recalcula saldo anterior (mesmo algoritmo do GET)
        var fechamentoAnterior = await _db.ConciliacoesDiarias
            .Where(c => c.ContaBancariaId == dto.ContaBancariaId
                     && c.TenantId == _t.TenantId
                     && c.DataConciliacao < dto.DataConciliacao)
            .OrderByDescending(c => c.DataConciliacao)
            .FirstOrDefaultAsync();

        decimal saldoAnterior;
        if (fechamentoAnterior != null)
            saldoAnterior = fechamentoAnterior.SaldoFinal;
        else
        {
            var entradasAnt = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == dto.ContaBancariaId && m.TenantId == _t.TenantId
                         && m.Tipo == "entrada" && m.DataMovimentacao < dto.DataConciliacao)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var saidasAnt = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == dto.ContaBancariaId && m.TenantId == _t.TenantId
                         && m.Tipo == "saida" && m.DataMovimentacao < dto.DataConciliacao)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var transEntAnt = await _db.MovimentacoesBancarias
                .Where(m => m.ContaDestinoId == dto.ContaBancariaId && m.TenantId == _t.TenantId
                         && m.Tipo == "transferencia" && m.DataMovimentacao < dto.DataConciliacao)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var transSaiAnt = await _db.MovimentacoesBancarias
                .Where(m => m.ContaBancariaId == dto.ContaBancariaId && m.TenantId == _t.TenantId
                         && m.Tipo == "transferencia" && m.DataMovimentacao < dto.DataConciliacao)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;

            saldoAnterior = conta.SaldoInicial + entradasAnt - saidasAnt + transEntAnt - transSaiAnt;
        }

        // Calcula totais do dia
        var movsDia = await _db.MovimentacoesBancarias
            .Where(m => m.ContaBancariaId == dto.ContaBancariaId
                     && m.TenantId == _t.TenantId
                     && m.DataMovimentacao == dto.DataConciliacao)
            .ToListAsync();

        var totalEntradas    = movsDia.Where(m => m.Tipo == "entrada").Sum(m => m.Valor);
        var totalSaidas      = movsDia.Where(m => m.Tipo == "saida").Sum(m => m.Valor);
        var totalTransfSaida = movsDia.Where(m => m.Tipo == "transferencia").Sum(m => m.Valor);
        var totalTransfEntrada = await _db.MovimentacoesBancarias
            .Where(m => m.ContaDestinoId == dto.ContaBancariaId
                     && m.TenantId == _t.TenantId
                     && m.DataMovimentacao == dto.DataConciliacao
                     && m.Tipo == "transferencia")
            .SumAsync(m => (decimal?)m.Valor) ?? 0;

        var saldoFinal = saldoAnterior + totalEntradas - totalSaidas
                       + totalTransfEntrada - totalTransfSaida;

        decimal? diferenca = dto.SaldoExtrato.HasValue
            ? saldoFinal - dto.SaldoExtrato.Value
            : null;

        // Cria registro de conciliação
        var conciliacao = new ConciliacaoDiaria
        {
            Id               = Guid.NewGuid(),
            TenantId         = _t.TenantId,
            ContaBancariaId  = dto.ContaBancariaId,
            DataConciliacao  = dto.DataConciliacao,
            SaldoAnterior    = saldoAnterior,
            TotalEntradas    = totalEntradas,
            TotalSaidas      = totalSaidas,
            SaldoFinal       = saldoFinal,
            SaldoExtrato     = dto.SaldoExtrato,
            Diferenca        = diferenca,
            Observacao       = dto.Observacao,
            FechadoPor       = _t.UserId,
            FechadoEm        = DateTime.UtcNow,
            CriadoEm        = DateTime.UtcNow
        };
        _db.ConciliacoesDiarias.Add(conciliacao);

        // Marca todas as movimentações do dia como conciliadas
        foreach (var m in movsDia)
            m.Conciliado = true;

        await _db.SaveChangesAsync();

        return Ok(new {
            mensagem = "Dia fechado com sucesso.",
            conciliacaoId = conciliacao.Id,
            saldoAnterior,
            totalEntradas,
            totalSaidas,
            saldoFinal,
            saldoExtrato = dto.SaldoExtrato,
            diferenca
        });
    }

    /// <summary>
    /// GET /api/bancario/conciliacao/historico/{contaId}
    /// Lista os fechamentos diários de uma conta.
    /// </summary>
    [HttpGet("conciliacao/historico/{contaId}")]
    public async Task<IActionResult> HistoricoConciliacao(
        Guid contaId,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate)
    {
        var inicio = de ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var fim    = ate ?? DateOnly.FromDateTime(DateTime.Today);

        var historico = await _db.ConciliacoesDiarias
            .Where(c => c.ContaBancariaId == contaId
                     && c.TenantId == _t.TenantId
                     && c.DataConciliacao >= inicio
                     && c.DataConciliacao <= fim)
            .OrderByDescending(c => c.DataConciliacao)
            .Select(c => new {
                c.Id,
                c.DataConciliacao,
                c.SaldoAnterior,
                c.TotalEntradas,
                c.TotalSaidas,
                c.SaldoFinal,
                c.SaldoExtrato,
                c.Diferenca,
                c.Observacao,
                c.FechadoEm
            })
            .ToListAsync();

        return Ok(historico);
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

        var contas = await _db.Contas
            .Include(c => c.Categoria)
            .Where(c => c.TenantId == _t.TenantId
                     && c.Status != "cancelada"
                     && c.DataCompetencia >= inicio
                     && c.DataCompetencia <= fim
                     && (
                         _db.OrdensServico.Any(os => os.Id == c.OsId &&
                             _db.Pets.Any(p => p.Id == os.PetId && p.TutorId == tutorId))
                         ||
                         _db.Vendas.Any(v => v.Id == c.VendaId && v.TutorId == tutorId)
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

        var total   = await query.CountAsync();
        var tutores = await query.OrderBy(t => t.Nome)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new {
                t.Id, t.Nome, t.Telefone,
                QtdPets = t.Pets.Count(p => p.Ativo)
            })
            .ToListAsync();

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
                             _db.OrdensServico.Any(os => os.Id == c.OsId &&
                                 _db.Pets.Any(p => p.Id == os.PetId && p.TutorId == t.Id))
                             ||
                             _db.Vendas.Any(v => v.Id == c.VendaId && v.TutorId == t.Id)
                         ))
                .SumAsync(c => (decimal?)(c.ValorPago ?? 0)) ?? 0;

            result.Add(new { t.Id, t.Nome, t.Telefone, t.QtdPets, TotalPago = totalPago });
        }

        return Ok(new { items = result, total, page, pageSize });
    }
}
