using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

public record ContratoCreate(
    Guid FornecedorId, Guid ProdutoId, decimal ValorTotal,
    Guid? CondicaoPagamentoId, int NumeroParcelas, DateOnly DataInicio, string? Obs);

public record MedirParcelaRequest(decimal? QuantidadeMedida, string? Obs);
public record RejeitarParcelaRequest(string Motivo);

[ApiController]
[Authorize]
[Route("api/contratos")]
public class ContratosController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public ContratosController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    // Medicao = qualquer usuario pode registrar. Aprovacao = so gestor.
    // Mesma regra ja usada em reabertura de caixa e aprovacao de campanha.
    private bool PodeAprovar => _t.Papel == "owner" || _t.Papel == "admin";

    // ── Fila de medicoes (cruza todos os contratos, pra nao precisar entrar
    // contrato por contrato pra fazer a rotina de medicao) ─────────────────

    [HttpGet("parcelas/pendentes")]
    public async Task<IActionResult> ParcelasPendentes([FromQuery] string? status)
    {
        var statusFiltro = string.IsNullOrEmpty(status)
            ? new[] { "pendente", "medido", "rejeitado" }
            : new[] { status };

        var lista = await _db.ContratoParcelas
            .Include(p => p.Contrato).ThenInclude(c => c!.Fornecedor)
            .Include(p => p.Contrato).ThenInclude(c => c!.Produto)
            .Where(p => p.TenantId == _t.TenantId
                     && p.Contrato!.Status == "ativo"
                     && statusFiltro.Contains(p.StatusMedicao))
            .OrderBy(p => p.DataPrevista)
            .Select(p => new {
                p.Id, p.ContratoId, p.Numero, p.ValorPrevisto, p.DataPrevista, p.StatusMedicao,
                p.QuantidadeMedida, p.ObsMedicao, p.MotivoRejeicao,
                FornecedorNome = p.Contrato!.Fornecedor != null ? p.Contrato.Fornecedor.Nome : null,
                ProdutoNome = p.Contrato.Produto != null ? p.Contrato.Produto.Nome : null
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Listagem e detalhe ──────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? status)
    {
        var q = _db.Contratos.Include(c => c.Fornecedor).Include(c => c.Produto).Where(c => c.TenantId == _t.TenantId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(c => c.Status == status);

        var lista = await q.OrderByDescending(c => c.CriadoEm)
            .Select(c => new {
                c.Id, c.ValorTotal, c.NumeroParcelas, c.DataInicio, c.Status,
                FornecedorNome = c.Fornecedor != null ? c.Fornecedor.Nome : null,
                ProdutoNome = c.Produto != null ? c.Produto.Nome : null,
                ParcelasPendentes = c.Parcelas.Count(p => p.StatusMedicao == "pendente"),
                ParcelasAprovadas = c.Parcelas.Count(p => p.StatusMedicao == "aprovado")
            })
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalhe(Guid id)
    {
        var c = await _db.Contratos
            .Include(x => x.Fornecedor)
            .Include(x => x.Produto)
            .Include(x => x.CondicaoPagamento)
            .Include(x => x.Parcelas)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (c == null) return NotFound();

        return Ok(new {
            c.Id, c.ValorTotal, c.NumeroParcelas, c.DataInicio, c.Status, c.Obs,
            Fornecedor = c.Fornecedor == null ? null : new { c.Fornecedor.Id, c.Fornecedor.Nome },
            Produto = c.Produto == null ? null : new { c.Produto.Id, c.Produto.Nome },
            CondicaoPagamento = c.CondicaoPagamento == null ? null : new { c.CondicaoPagamento.Id, c.CondicaoPagamento.Nome },
            Parcelas = c.Parcelas.OrderBy(p => p.Numero).Select(p => new {
                p.Id, p.Numero, p.ValorPrevisto, p.DataPrevista, p.StatusMedicao,
                p.QuantidadeMedida, p.ObsMedicao, p.MedidoEm, p.AprovadoEm, p.MotivoRejeicao, p.ContaId
            })
        });
    }

    // ── Criacao ──────────────────────────────────────────────────────────
    // Gera de uma vez as N parcelas + os titulos em "previsao" no financeiro
    // (mesmo padrao de rateio usado em ComprasController.Confirmar, so que
    // aqui os titulos nascem em "previsao", nao "aberta").

    [HttpPost]
    public async Task<IActionResult> Criar(ContratoCreate dto)
    {
        if (dto.NumeroParcelas < 1)
            return BadRequest(new { erro = "Numero de parcelas deve ser pelo menos 1." });
        if (dto.ValorTotal <= 0)
            return BadRequest(new { erro = "Valor total deve ser positivo." });

        var fornecedor = await _db.Fornecedores.FirstOrDefaultAsync(f => f.Id == dto.FornecedorId && f.TenantId == _t.TenantId);
        if (fornecedor == null) return BadRequest(new { erro = "Fornecedor nao encontrado." });

        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == dto.ProdutoId && p.TenantId == _t.TenantId);
        if (produto == null) return BadRequest(new { erro = "Produto nao encontrado no cadastro." });

        CondicaoPagamento? condicao = null;
        if (dto.CondicaoPagamentoId.HasValue)
            condicao = await _db.CondicoesPagamento.FirstOrDefaultAsync(c => c.Id == dto.CondicaoPagamentoId.Value && c.TenantId == _t.TenantId);

        var contrato = new Contrato
        {
            Id = Guid.NewGuid(), TenantId = _t.TenantId,
            FornecedorId = dto.FornecedorId, ProdutoId = dto.ProdutoId,
            ValorTotal = dto.ValorTotal, CondicaoPagamentoId = dto.CondicaoPagamentoId,
            NumeroParcelas = dto.NumeroParcelas, DataInicio = dto.DataInicio,
            Status = "ativo", Obs = dto.Obs,
            CriadoPor = _t.UserId, CriadoEm = DateTime.UtcNow, AtualizadoEm = DateTime.UtcNow
        };
        _db.Contratos.Add(contrato);

        var intervalo = condicao?.IntervaloDias ?? 30;
        var valorParcela = Math.Round(dto.ValorTotal / dto.NumeroParcelas, 2);

        for (int i = 0; i < dto.NumeroParcelas; i++)
        {
            var valor = (i == dto.NumeroParcelas - 1)
                ? dto.ValorTotal - (valorParcela * (dto.NumeroParcelas - 1))
                : valorParcela;
            var dataPrevista = dto.DataInicio.AddDays(intervalo * (i + 1));

            var contaId = Guid.NewGuid();
            _db.Contas.Add(new Conta
            {
                Id = contaId, TenantId = _t.TenantId,
                Tipo = "despesa",
                Descricao = $"Contrato - {fornecedor.Nome} ({produto.Nome}) - parcela {i + 1}/{dto.NumeroParcelas}",
                Valor = valor,
                DataCompetencia = dto.DataInicio,
                DataVencimento = dataPrevista,
                Status = "previsao", // <- nao pode ser baixado direto, so via aprovacao da medicao
                CriadoPor = _t.UserId, CriadoEm = DateTime.UtcNow, AtualizadoEm = DateTime.UtcNow
            });

            contrato.Parcelas.Add(new ContratoParcela
            {
                Id = Guid.NewGuid(), TenantId = _t.TenantId, ContratoId = contrato.Id,
                Numero = i + 1, ValorPrevisto = valor, DataPrevista = dataPrevista,
                ContaId = contaId, StatusMedicao = "pendente"
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { contrato.Id });
    }

    [HttpPut("{id}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        if (!PodeAprovar) return Forbid();

        var contrato = await _db.Contratos
            .Include(c => c.Parcelas)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (contrato == null) return NotFound();

        contrato.Status = "cancelado";
        contrato.AtualizadoEm = DateTime.UtcNow;

        // Cancela tambem os titulos em previsao das parcelas que ainda nao foram aprovadas
        var contaIds = contrato.Parcelas.Where(p => p.StatusMedicao != "aprovado").Select(p => p.ContaId).ToList();
        var contas = await _db.Contas.Where(c => contaIds.Contains(c.Id) && c.Status == "previsao").ToListAsync();
        foreach (var c in contas) c.Status = "cancelada";

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Medicao ──────────────────────────────────────────────────────────

    [HttpPost("{id}/parcelas/{parcelaId}/medir")]
    public async Task<IActionResult> Medir(Guid id, Guid parcelaId, MedirParcelaRequest dto)
    {
        var parcela = await _db.ContratoParcelas
            .FirstOrDefaultAsync(p => p.Id == parcelaId && p.ContratoId == id && p.TenantId == _t.TenantId);
        if (parcela == null) return NotFound();

        if (parcela.StatusMedicao != "pendente" && parcela.StatusMedicao != "rejeitado")
            return BadRequest(new { erro = $"Parcela ja esta '{parcela.StatusMedicao}'." });

        parcela.StatusMedicao = "medido";
        parcela.QuantidadeMedida = dto.QuantidadeMedida;
        parcela.ObsMedicao = dto.Obs;
        parcela.MedidoPor = _t.UserId;
        parcela.MedidoEm = DateTime.UtcNow;
        parcela.MotivoRejeicao = null;

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Medicao registrada. Aguardando aprovacao do gestor." });
    }

    // ── Aprovacao (libera o titulo da previsao) ─────────────────────────

    [HttpPost("{id}/parcelas/{parcelaId}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, Guid parcelaId)
    {
        if (!PodeAprovar) return Forbid();

        var parcela = await _db.ContratoParcelas
            .FirstOrDefaultAsync(p => p.Id == parcelaId && p.ContratoId == id && p.TenantId == _t.TenantId);
        if (parcela == null) return NotFound();

        if (parcela.StatusMedicao != "medido")
            return BadRequest(new { erro = "Só é possível aprovar uma parcela que já foi medida." });

        parcela.StatusMedicao = "aprovado";
        parcela.AprovadoPor = _t.UserId;
        parcela.AprovadoEm = DateTime.UtcNow;

        // Aqui que o titulo sai de "previsao" e fica pagavel de verdade.
        if (parcela.ContaId.HasValue)
        {
            var conta = await _db.Contas.FirstOrDefaultAsync(c => c.Id == parcela.ContaId.Value);
            if (conta != null && conta.Status == "previsao")
            {
                conta.Status = "aberta";
                conta.AtualizadoEm = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Medicao aprovada. Titulo liberado no financeiro." });
    }

    [HttpPost("{id}/parcelas/{parcelaId}/rejeitar")]
    public async Task<IActionResult> Rejeitar(Guid id, Guid parcelaId, RejeitarParcelaRequest dto)
    {
        if (!PodeAprovar) return Forbid();

        var parcela = await _db.ContratoParcelas
            .FirstOrDefaultAsync(p => p.Id == parcelaId && p.ContratoId == id && p.TenantId == _t.TenantId);
        if (parcela == null) return NotFound();

        if (parcela.StatusMedicao != "medido")
            return BadRequest(new { erro = "Só é possível rejeitar uma parcela que já foi medida." });

        parcela.StatusMedicao = "rejeitado";
        parcela.MotivoRejeicao = dto.Motivo;
        // Titulo continua em "previsao" -- precisa medir de novo antes de aprovar.

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Medicao rejeitada. O responsavel precisa medir novamente." });
    }
}
