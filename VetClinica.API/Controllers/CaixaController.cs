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
[Route("api/caixa")]
public class CaixaController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;

    public CaixaController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    // ── Hoje ──────────────────────────────────────────────────────
    [HttpGet("hoje")]
    public async Task<IActionResult> Hoje()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var cx = await _db.Caixas
            .Include(c => c.Movimentacoes)
            .FirstOrDefaultAsync(c => c.TenantId == _t.TenantId && c.Data == hoje);

        if (cx == null) return Ok(new { aberto = false });

        var movs = cx.Movimentacoes.Select(m => new MovimentacaoDto(
            m.Id, m.Tipo, m.Valor, m.Descricao, m.CriadoEm)).ToList();

        return Ok(new CaixaDetalheDto(cx.Id, cx.Data, cx.SaldoInicial,
            cx.TotalVendas, cx.TotalServicos, cx.SaldoFinal,
            cx.FechadoEm == null, cx.AbertoEm, cx.FechadoEm, movs));
    }

    // ── Abrir ─────────────────────────────────────────────────────
    // Pendência #3: saldo inicial = saldo final do dia anterior
    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir(AbrirCaixaRequest dto)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (await _db.Caixas.AnyAsync(c => c.TenantId == _t.TenantId && c.Data == hoje))
            return BadRequest(new { erro = "Caixa de hoje já foi aberto" });

        // Busca saldo final do dia anterior automaticamente
        var ontem = hoje.AddDays(-1);
        var caixaAnterior = await _db.Caixas
            .FirstOrDefaultAsync(c => c.TenantId == _t.TenantId && c.Data == ontem);

        // Saldo inicial = saldo final do dia anterior (se existir), senão usa o informado
        var saldoInicial = caixaAnterior?.SaldoFinal ?? dto.SaldoInicial;

        var cx = new Caixa
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            UserId = _t.UserId,
            Data = hoje,
            SaldoInicial = saldoInicial,
            AbertoEm = DateTime.UtcNow
        };
        _db.Caixas.Add(cx);
        await _db.SaveChangesAsync();

        return Ok(new { cx.Id, saldoInicial });
    }

    // ── Movimentações: Retirada e Depósito (Pendência #4) ─────────
    [HttpPost("movimentacao")]
    public async Task<IActionResult> Movimentacao(MovimentacaoRequest dto)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var cx = await _db.Caixas
            .FirstOrDefaultAsync(c => c.TenantId == _t.TenantId && c.Data == hoje);

        if (cx == null) return BadRequest(new { erro = "Nenhum caixa aberto hoje" });
        if (cx.FechadoEm != null) return BadRequest(new { erro = "Caixa já fechado" });

        if (!new[] { "retirada", "deposito" }.Contains(dto.Tipo))
            return BadRequest(new { erro = "Tipo inválido. Use: retirada ou deposito" });

        var mov = new MovimentacaoCaixa
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            CaixaId = cx.Id,
            UserId = _t.UserId,
            Tipo = dto.Tipo,
            Valor = dto.Valor,
            Descricao = dto.Descricao,
            CriadoEm = DateTime.UtcNow
        };
        _db.MovimentacoesCaixa.Add(mov);
        await _db.SaveChangesAsync();

        return Ok(new MovimentacaoDto(mov.Id, mov.Tipo, mov.Valor, mov.Descricao, mov.CriadoEm));
    }

    // ── Fechar (Pendências #1, #2, #5, #6) ───────────────────────
    [HttpPut("fechar")]
    public async Task<IActionResult> Fechar(FecharCaixaRequest dto)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var cx = await _db.Caixas
            .Include(c => c.Movimentacoes)
            .FirstOrDefaultAsync(c => c.TenantId == _t.TenantId && c.Data == hoje);

        if (cx == null) return NotFound(new { erro = "Nenhum caixa aberto hoje" });
        // Pendência #2: caixa fechado não pode reabrir sem aprovação
        if (cx.FechadoEm != null) return BadRequest(new { erro = "Caixa já fechado" });

        var inicioUtc = DateTime.Today.ToUniversalTime();
        var fimUtc = inicioUtc.AddDays(1);

        // Totaliza vendas e serviços do dia
        cx.TotalVendas = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId && v.Status == "finalizada"
                     && v.CriadoEm >= inicioUtc && v.CriadoEm < fimUtc)
            .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

        cx.TotalServicos = await _db.OrdensServico
            .Where(o => o.TenantId == _t.TenantId && o.EntregueEm != null
                     && o.EntregueEm >= inicioUtc && o.EntregueEm < fimUtc)
            .SumAsync(o => (decimal?)o.ValorTotal) ?? 0;

        // Pendência #5: matemática correta do fechamento
        // saldo final = saldo inicial + entradas dinheiro - retiradas - contas pagas em dinheiro
        var entradasDinheiro = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId && v.Status == "finalizada"
                     && v.FormaPagamento == "Dinheiro"
                     && v.CriadoEm >= inicioUtc && v.CriadoEm < fimUtc)
            .SumAsync(v => (decimal?)v.ValorTotal) ?? 0;

        // NOTA: serviços (OS) são cobrados via PDV, virando Venda com forma de pagamento.
        // Portanto já entram em entradasDinheiro quando pagos em dinheiro. NÃO somar OS
        // separadamente aqui — isso duplicaria o serviço no saldo físico.

        var retiradas = cx.Movimentacoes
            .Where(m => m.Tipo == "retirada").Sum(m => m.Valor);

        var depositos = cx.Movimentacoes
            .Where(m => m.Tipo == "deposito").Sum(m => m.Valor);

        var contasPagasDinheiro = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId
                     && c.Tipo == "despesa"
                     && c.FormaPagamento == "Dinheiro"
                     && c.DataBaixa == hoje)
            .SumAsync(c => (decimal?)c.ValorPago) ?? 0;

        // Saldo final = saldo inicial + entradas dinheiro + depósitos - retiradas - contas pagas em dinheiro
        var saldoFinalCalculado = cx.SaldoInicial
            + entradasDinheiro
            + depositos
            - retiradas
            - contasPagasDinheiro;

        cx.SaldoFinal = dto.SaldoFinal ?? saldoFinalCalculado;
        cx.FechadoEm = DateTime.UtcNow;
        cx.Obs = dto.Obs;

        // ── Pendência #1: fechamento alimenta o Financeiro ───────────────
        // Agrupa vendas + serviços por forma de pagamento e cria uma Conta por forma
        var vendasPorForma = await _db.Vendas
            .Where(v => v.TenantId == _t.TenantId && v.Status == "finalizada"
                     && v.CriadoEm >= inicioUtc && v.CriadoEm < fimUtc
                     && v.FormaPagamento != null)
            .GroupBy(v => v.FormaPagamento!)
            .Select(g => new { Forma = g.Key, Total = g.Sum(v => v.ValorTotal) })
            .ToListAsync();

        // Busca conta bancária padrão do tenant (se houver)
        var contaBancariaDefault = await _db.ContasBancarias
            .Where(c => c.TenantId == _t.TenantId && c.Ativo)
            .OrderBy(c => c.CriadoEm)
            .FirstOrDefaultAsync();

        foreach (var grupo in vendasPorForma.Where(g => g.Total > 0))
        {
            var conta = new Conta
            {
                Id              = Guid.NewGuid(),
                TenantId        = _t.TenantId,
                Tipo            = "receita",
                Descricao       = $"Fechamento de caixa — {hoje:dd/MM/yyyy} ({grupo.Forma})",
                Valor           = grupo.Total,
                DataCompetencia = hoje,
                DataVencimento  = hoje,
                FormaPagamento  = grupo.Forma,
                Status          = "recebida",
                ValorPago       = grupo.Total,
                DataBaixa       = hoje,
                ContaBancaria   = contaBancariaDefault?.Nome,
                CriadoPor       = _t.UserId,
                CriadoEm        = DateTime.UtcNow,
                AtualizadoEm    = DateTime.UtcNow
            };
            _db.Contas.Add(conta);
            await _db.SaveChangesAsync(); // salva para ter ID

            // Cria MovimentacaoBancaria se tiver conta bancária configurada
            if (contaBancariaDefault != null)
            {
                _db.MovimentacoesBancarias.Add(new MovimentacaoBancaria
                {
                    Id               = Guid.NewGuid(),
                    TenantId         = _t.TenantId,
                    ContaBancariaId  = contaBancariaDefault.Id,
                    Tipo             = "entrada",
                    Valor            = grupo.Total,
                    Descricao        = $"Caixa {hoje:dd/MM/yyyy} — {grupo.Forma}",
                    DataMovimentacao = hoje,
                    ContaId          = conta.Id,
                    Origem           = "caixa",
                    Conciliado       = false,
                    CriadoPor        = _t.UserId,
                    CriadoEm        = DateTime.UtcNow
                });
            }
        }

        // Retiradas viram despesas no financeiro
        foreach (var retirada in cx.Movimentacoes.Where(m => m.Tipo == "retirada" && m.Valor > 0))
        {
            _db.Contas.Add(new Conta
            {
                Id              = Guid.NewGuid(),
                TenantId        = _t.TenantId,
                Tipo            = "despesa",
                Descricao       = $"Retirada de caixa — {retirada.Descricao ?? hoje.ToString("dd/MM/yyyy")}",
                Valor           = retirada.Valor,
                DataCompetencia = hoje,
                DataVencimento  = hoje,
                FormaPagamento  = "Dinheiro",
                Status          = "paga",
                ValorPago       = retirada.Valor,
                DataBaixa       = hoje,
                CriadoPor       = _t.UserId,
                CriadoEm        = DateTime.UtcNow,
                AtualizadoEm    = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        // Pendência #6: saldo final vira inicial do próximo dia (automático via endpoint Abrir)
        // Já resolvido: o endpoint Abrir busca SaldoFinal do dia anterior automaticamente

        return Ok(new
        {
            cx.Id,
            cx.SaldoInicial,
            cx.TotalVendas,
            cx.TotalServicos,
            cx.SaldoFinal,
            SaldoCalculado = saldoFinalCalculado,
            Retiradas = retiradas,
            Depositos = depositos,
            ContasPagasDinheiro = contasPagasDinheiro
        });
    }

    // ── Reabrir com aprovação (Pendência #2) ──────────────────────
    // Restrito a owner/admin. Estorna os lançamentos do fechamento anterior
    // para evitar duplicação quando o caixa for fechado novamente.
    [HttpPost("{id:guid}/reabrir")]
    public async Task<IActionResult> Reabrir(Guid id)
    {
        if (_t.Papel != "owner" && _t.Papel != "admin")
            return StatusCode(403, new { erro = "Apenas owner ou admin podem reabrir um caixa fechado." });

        var cx = await _db.Caixas
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);

        if (cx == null) return NotFound();
        if (cx.FechadoEm == null) return BadRequest(new { erro = "Caixa não está fechado" });

        // Estorna lançamentos gerados pelo fechamento deste dia (idempotência)
        var prefixoFechamento = $"Fechamento de caixa — {cx.Data:dd/MM/yyyy}";
        var contasDoFechamento = await _db.Contas
            .Where(c => c.TenantId == _t.TenantId
                     && c.DataCompetencia == cx.Data
                     && (c.Descricao.StartsWith(prefixoFechamento)
                         || c.Descricao.StartsWith("Retirada de caixa —")))
            .ToListAsync();

        var movsBancarias = await _db.MovimentacoesBancarias
            .Where(m => m.TenantId == _t.TenantId
                     && m.Origem == "caixa"
                     && m.DataMovimentacao == cx.Data)
            .ToListAsync();

        if (movsBancarias.Count > 0) _db.MovimentacoesBancarias.RemoveRange(movsBancarias);
        if (contasDoFechamento.Count > 0) _db.Contas.RemoveRange(contasDoFechamento);

        // Reabre o caixa e registra auditoria
        cx.FechadoEm = null;
        cx.SaldoFinal = null;
        cx.RequerAprovacao = true;
        cx.AprovadoPor = _t.UserId;
        cx.AprovadoEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new
        {
            mensagem = "Caixa reaberto. Lançamentos do fechamento anterior foram estornados e a ação registrada para auditoria.",
            lancamentosEstornados = contasDoFechamento.Count,
            movimentacoesBancariasEstornadas = movsBancarias.Count
        });
    }

    // ── Histórico ─────────────────────────────────────────────────
    [HttpGet("historico")]
    public async Task<IActionResult> Historico([FromQuery] int ultimos = 30)
    {
        var items = await _db.Caixas
            .Where(c => c.TenantId == _t.TenantId)
            .OrderByDescending(c => c.Data)
            .Take(ultimos)
            .Select(c => new CaixaDto(c.Id, c.Data, c.SaldoInicial, c.TotalVendas,
                c.TotalServicos, c.SaldoFinal, c.FechadoEm == null, c.AbertoEm, c.FechadoEm))
            .ToListAsync();
        return Ok(items);
    }
}
