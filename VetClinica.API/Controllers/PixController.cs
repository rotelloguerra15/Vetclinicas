using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;
using VetClinica.API.Services.Payments;

namespace VetClinica.API.Controllers;

[ApiController]
[Route("api/pix")]
public class PixController : ControllerBase
{
    private readonly TenantDbContextFactory _factory;
    private readonly TenantContext          _t;
    private readonly PlatformDbContext      _platform;
    private readonly IPaymentProvider       _pay;
    private readonly ILogger<PixController>  _logger;

    public PixController(TenantDbContextFactory factory, TenantContext t,
        PlatformDbContext platform, IPaymentProvider pay, ILogger<PixController> logger)
    { _factory = factory; _t = t; _platform = platform; _pay = pay; _logger = logger; }

    public record CriarPixRequest(decimal Valor, string? Descricao, Guid? VendaId);

    // ── Criar cobrança Pix (autenticado) ──────────────────────────
    [Authorize]
    [HttpPost("cobranca")]
    public async Task<IActionResult> Criar(CriarPixRequest dto)
    {
        if (dto.Valor <= 0) return BadRequest(new { erro = "Valor inválido." });

        var db = _factory.Create();
        var par = await db.ParametrosSistema.FirstOrDefaultAsync(p => p.TenantId == _t.TenantId);

        if (par == null || !par.AsaasPixAtivo || string.IsNullOrWhiteSpace(par.AsaasApiKey))
            return BadRequest(new { erro = "Pix não habilitado. Configure o Asaas nos parâmetros da clínica." });

        var cobranca = new CobrancaPix
        {
            Id        = Guid.NewGuid(),
            TenantId  = _t.TenantId,
            VendaId   = dto.VendaId,
            Valor     = dto.Valor,
            Descricao = dto.Descricao ?? "Pagamento PDV",
            Status    = "pendente",
            Provider  = _pay.Nome,
            CriadoPor = _t.UserId,
            CriadoEm  = DateTime.UtcNow
        };

        // externalReference carrega o tenant para o webhook (que não tem JWT)
        var externalRef = $"{_t.TenantId}|{cobranca.Id}";

        var r = await _pay.CriarCobrancaPixAsync(
            par.AsaasApiKey!, par.AsaasAmbiente ?? "sandbox",
            dto.Valor, cobranca.Descricao!, externalRef);

        if (!r.Ok)
        {
            cobranca.Status = "erro";
            db.CobrancasPix.Add(cobranca);
            await db.SaveChangesAsync();
            return BadRequest(new { erro = r.Erro });
        }

        cobranca.ProviderChargeId = r.ChargeId;
        cobranca.QrPayload = r.QrPayload;
        cobranca.QrImage   = r.QrImage;
        cobranca.ExpiraEm  = r.ExpiraEm;
        db.CobrancasPix.Add(cobranca);
        await db.SaveChangesAsync();

        return Ok(new
        {
            cobranca.Id,
            cobranca.Valor,
            cobranca.Status,
            qrPayload = cobranca.QrPayload,
            qrImage   = cobranca.QrImage,
            expiraEm  = cobranca.ExpiraEm
        });
    }

    // ── Consultar status (autenticado) — usado pelo polling do PDV ──
    [Authorize]
    [HttpGet("cobranca/{id:guid}")]
    public async Task<IActionResult> Status(Guid id)
    {
        var db = _factory.Create();
        var cob = await db.CobrancasPix.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (cob == null) return NotFound();

        // Se ainda pendente, confirma no provider (fallback caso o webhook atrase)
        if (cob.Status == "pendente" && !string.IsNullOrWhiteSpace(cob.ProviderChargeId))
        {
            var par = await db.ParametrosSistema.FirstOrDefaultAsync(p => p.TenantId == _t.TenantId);
            if (par?.AsaasApiKey != null)
            {
                var st = await _pay.ConsultarPixAsync(par.AsaasApiKey, par.AsaasAmbiente ?? "sandbox", cob.ProviderChargeId!);
                if (st.Ok && st.Status == "pago")
                {
                    await ConfirmarPagamento(db, cob);
                    await db.SaveChangesAsync();
                }
                else if (st.Ok && st.Status != "pendente")
                {
                    cob.Status = st.Status;
                    await db.SaveChangesAsync();
                }
            }
        }

        return Ok(new { cob.Id, cob.Status, cob.Valor, pago = cob.Status == "pago", cob.PagoEm });
    }

    // ── Webhook do Asaas (anônimo; resolve tenant pela externalReference) ──
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        try
        {
            var root = JsonDocument.Parse(raw).RootElement;
            var evento = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;
            if (!root.TryGetProperty("payment", out var pay))
                return Ok(new { ignored = true });

            var chargeId    = pay.TryGetProperty("id", out var pid) ? pid.GetString() : null;
            var externalRef = pay.TryGetProperty("externalReference", out var er) ? er.GetString() : null;
            var statusProv  = pay.TryGetProperty("status", out var ps) ? ps.GetString() : null;

            if (string.IsNullOrWhiteSpace(externalRef) || !externalRef.Contains('|'))
                return Ok(new { ignored = true, motivo = "sem externalReference" });

            // externalRef = "{tenantId}|{cobrancaId}"
            var partes = externalRef.Split('|');
            if (!Guid.TryParse(partes[0], out var tenantId))
                return Ok(new { ignored = true });

            // Resolve o schema do tenant (webhook não tem JWT)
            var tenant = await _platform.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.SchemaName))
                return Ok(new { ignored = true, motivo = "tenant não encontrado" });

            using var db = _factory.CreateForSchema(tenant.SchemaName);
            var cob = await db.CobrancasPix.FirstOrDefaultAsync(c =>
                c.TenantId == tenantId && (c.ProviderChargeId == chargeId || c.Id.ToString() == partes[1]));
            if (cob == null) return Ok(new { ignored = true, motivo = "cobrança não encontrada" });

            var statusNorm = _pay.NormalizarStatus(statusProv ?? "");
            var pago = statusNorm == "pago"
                       || evento is "PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED";

            if (pago && cob.Status != "pago")
            {
                await ConfirmarPagamento(db, cob);
                await db.SaveChangesAsync();
                _logger.LogInformation("[Pix] Pago via webhook cobranca={id} tenant={t}", cob.Id, tenantId);
            }
            else if (!pago && statusNorm != "pendente" && cob.Status == "pendente")
            {
                cob.Status = statusNorm;
                await db.SaveChangesAsync();
            }

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pix] webhook erro: {msg}", ex.Message);
            // Retorna 200 para o Asaas não reenfileirar infinitamente em payload malformado
            return Ok(new { ok = false });
        }
    }

    // ── Marca a cobrança como paga e dá baixa na Conta vinculada (idempotente) ──
    private async Task ConfirmarPagamento(TenantDbContext db, CobrancaPix cob)
    {
        cob.Status  = "pago";
        cob.PagoEm  = DateTime.UtcNow;

        if (cob.VendaId == null) return;

        var hoje  = DateOnly.FromDateTime(DateTime.Today);
        var conta = await db.Contas.FirstOrDefaultAsync(c => c.VendaId == cob.VendaId && c.TenantId == cob.TenantId);

        // Só baixa se ainda estiver aberta (idempotência: não duplica se já recebida)
        if (conta == null || conta.Status == "recebida") return;

        conta.Status       = "recebida";
        conta.ValorPago    = conta.Valor;
        conta.DataBaixa    = hoje;
        conta.FormaPagamento = "PIX";
        conta.AtualizadoEm = DateTime.UtcNow;

        // Lançamento contábil
        db.Lancamentos.Add(new Lancamento
        {
            Id          = Guid.NewGuid(),
            TenantId    = cob.TenantId,
            OsId        = null,
            CategoriaId = conta.CategoriaId,
            Data        = hoje,
            Tipo        = "receita",
            Valor       = conta.Valor,
            Descricao   = $"Baixa Pix: {conta.Descricao}",
            CriadoEm    = DateTime.UtcNow
        });

        // Movimentação bancária na conta padrão (se houver)
        var contaBancaria = await db.ContasBancarias
            .Where(c => c.TenantId == cob.TenantId && c.Ativo)
            .OrderBy(c => c.CriadoEm)
            .FirstOrDefaultAsync();

        if (contaBancaria != null)
        {
            db.MovimentacoesBancarias.Add(new MovimentacaoBancaria
            {
                Id               = Guid.NewGuid(),
                TenantId         = cob.TenantId,
                ContaBancariaId  = contaBancaria.Id,
                Tipo             = "entrada",
                Valor            = conta.Valor,
                Descricao        = $"Pix recebido — {conta.Descricao}",
                DataMovimentacao = hoje,
                ContaId          = conta.Id,
                Origem           = "pix",
                Conciliado       = true,
                CriadoPor        = cob.CriadoPor,
                CriadoEm         = DateTime.UtcNow
            });
        }
    }
}
