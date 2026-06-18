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
[Route("api/pagamentos")]
public class PagamentosController : ControllerBase
{
    private readonly TenantDbContextFactory _factory;
    private readonly TenantContext          _t;
    private readonly PlatformDbContext      _platform;
    private readonly MercadoPagoProvider    _mp;
    private readonly ILogger<PagamentosController> _logger;

    public PagamentosController(TenantDbContextFactory factory, TenantContext t,
        PlatformDbContext platform, MercadoPagoProvider mp, ILogger<PagamentosController> logger)
    { _factory = factory; _t = t; _platform = platform; _mp = mp; _logger = logger; }

    public record CobrarRequest(decimal Valor, string? Descricao, Guid? VendaId);

    private string WebhookUrl(Guid tenantId) =>
        $"{Request.Scheme}://{Request.Host}/api/pagamentos/webhook/{tenantId}";

    // ── Listar maquininhas Point (para o painel de config) ────────────
    [Authorize]
    [HttpGet("devices")]
    public async Task<IActionResult> Devices()
    {
        var db = _factory.Create();
        var par = await db.ParametrosSistema.FirstOrDefaultAsync(p => p.TenantId == _t.TenantId);
        if (par?.MpAccessToken == null)
            return BadRequest(new { erro = "Mercado Pago não configurado." });

        var devices = await _mp.ListarDispositivosAsync(par.MpAccessToken);
        return Ok(devices.Select(d => new { d.Id, d.Name }));
    }

    // ── PIX (QR dinâmico) ─────────────────────────────────────────────
    [Authorize]
    [HttpPost("pix")]
    public async Task<IActionResult> Pix(CobrarRequest dto)
    {
        if (dto.Valor <= 0) return BadRequest(new { erro = "Valor inválido." });

        var db = _factory.Create();
        var par = await db.ParametrosSistema.FirstOrDefaultAsync(p => p.TenantId == _t.TenantId);
        if (par == null || !par.MpAtivo || string.IsNullOrWhiteSpace(par.MpAccessToken))
            return BadRequest(new { erro = "Mercado Pago não habilitado. Configure nos parâmetros da clínica." });

        var cob = NovaCobranca(dto, "pix");
        var externalRef = $"{_t.TenantId}|{cob.Id}";

        var r = await _mp.CriarPixAsync(par.MpAccessToken!, dto.Valor, cob.Descricao!, externalRef, WebhookUrl(_t.TenantId));
        if (!r.Ok)
        {
            cob.Status = "erro";
            db.CobrancasPix.Add(cob);
            await db.SaveChangesAsync();
            return BadRequest(new { erro = r.Erro });
        }

        cob.ProviderChargeId = r.PaymentId;
        cob.QrPayload = r.QrPayload;
        cob.QrImage   = r.QrImage;
        cob.ExpiraEm  = r.ExpiraEm;
        db.CobrancasPix.Add(cob);
        await db.SaveChangesAsync();

        return Ok(new { cob.Id, cob.Valor, cob.Status, qrPayload = cob.QrPayload, qrImage = cob.QrImage, expiraEm = cob.ExpiraEm });
    }

    // ── CARTÃO (maquininha Point) ─────────────────────────────────────
    [Authorize]
    [HttpPost("cartao")]
    public async Task<IActionResult> Cartao(CobrarRequest dto)
    {
        if (dto.Valor <= 0) return BadRequest(new { erro = "Valor inválido." });

        var db = _factory.Create();
        var par = await db.ParametrosSistema.FirstOrDefaultAsync(p => p.TenantId == _t.TenantId);
        if (par == null || !par.MpAtivo || string.IsNullOrWhiteSpace(par.MpAccessToken))
            return BadRequest(new { erro = "Mercado Pago não habilitado." });
        if (string.IsNullOrWhiteSpace(par.MpDeviceId))
            return BadRequest(new { erro = "Nenhuma maquininha selecionada nos parâmetros da clínica." });

        var cob = NovaCobranca(dto, "cartao");
        var externalRef = $"{_t.TenantId}|{cob.Id}";

        var r = await _mp.CriarOrdemCartaoAsync(par.MpAccessToken!, par.MpDeviceId!, dto.Valor, cob.Descricao!, externalRef);
        if (!r.Ok)
        {
            cob.Status = "erro";
            db.CobrancasPix.Add(cob);
            await db.SaveChangesAsync();
            return BadRequest(new { erro = r.Erro });
        }

        cob.ProviderChargeId = r.IntentId;
        db.CobrancasPix.Add(cob);
        await db.SaveChangesAsync();

        return Ok(new { cob.Id, cob.Valor, cob.Status, mensagem = "Cobrança enviada para a maquininha. Aguardando o cartão do cliente." });
    }

    // ── Status (polling do PDV) ───────────────────────────────────────
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Status(Guid id)
    {
        var db = _factory.Create();
        var cob = await db.CobrancasPix.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (cob == null) return NotFound();
        return Ok(new { cob.Id, cob.Status, cob.Valor, pago = cob.Status == "pago", cob.PagoEm });
    }

    // ── Webhook por tenant (anônimo) ──────────────────────────────────
    // MP chama esta URL (definida em notification_url / webhook da conta).
    [AllowAnonymous]
    [HttpPost("webhook/{tenantId:guid}")]
    public async Task<IActionResult> Webhook(Guid tenantId)
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        try
        {
            var root = JsonDocument.Parse(raw).RootElement;
            var tipo = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (tipo != "payment" || !root.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var pid))
                return Ok(new { ignored = true });

            var paymentId = pid.GetString() ?? pid.GetRawText();

            var tenant = await _platform.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId);
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.SchemaName))
                return Ok(new { ignored = true });

            using var db = _factory.CreateForSchema(tenant.SchemaName);
            var par = await db.ParametrosSistema.FirstOrDefaultAsync(p => p.TenantId == tenantId);
            if (par?.MpAccessToken == null) return Ok(new { ignored = true });

            // Busca o pagamento no MP para saber status + external_reference
            var info = await _mp.ConsultarPagamentoAsync(par.MpAccessToken, paymentId);
            if (!info.Ok || string.IsNullOrWhiteSpace(info.ExternalReference) || !info.ExternalReference.Contains('|'))
                return Ok(new { ignored = true });

            var cobId = info.ExternalReference.Split('|')[1];
            var cob = await db.CobrancasPix.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id.ToString() == cobId);
            if (cob == null) return Ok(new { ignored = true });

            if (info.Status == "pago" && cob.Status != "pago")
            {
                cob.ProviderChargeId = paymentId;
                await ConfirmarPagamento(db, cob);
                await db.SaveChangesAsync();
                _logger.LogInformation("[MP] Pago via webhook cobranca={id} tenant={t}", cob.Id, tenantId);
            }
            else if (info.Status is "cancelado" or "erro" && cob.Status == "pendente")
            {
                cob.Status = info.Status;
                await db.SaveChangesAsync();
            }

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MP] webhook erro: {msg}", ex.Message);
            return Ok(new { ok = false });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private CobrancaPix NovaCobranca(CobrarRequest dto, string metodo) => new()
    {
        Id        = Guid.NewGuid(),
        TenantId  = _t.TenantId,
        VendaId   = dto.VendaId,
        Valor     = dto.Valor,
        Descricao = dto.Descricao ?? $"Pagamento PDV ({metodo})",
        Status    = "pendente",
        Provider  = "mercadopago",
        CriadoPor = _t.UserId,
        CriadoEm  = DateTime.UtcNow
    };

    private async Task ConfirmarPagamento(TenantDbContext db, CobrancaPix cob)
    {
        cob.Status = "pago";
        cob.PagoEm = DateTime.UtcNow;
        if (cob.VendaId == null) return;

        var hoje  = DateOnly.FromDateTime(DateTime.Today);
        var conta = await db.Contas.FirstOrDefaultAsync(c => c.VendaId == cob.VendaId && c.TenantId == cob.TenantId);
        if (conta == null || conta.Status == "recebida") return;

        conta.Status         = "recebida";
        conta.ValorPago      = conta.Valor;
        conta.DataBaixa      = hoje;
        conta.AtualizadoEm   = DateTime.UtcNow;

        db.Lancamentos.Add(new Lancamento
        {
            Id = Guid.NewGuid(), TenantId = cob.TenantId, OsId = null,
            CategoriaId = conta.CategoriaId, Data = hoje, Tipo = "receita",
            Valor = conta.Valor, Descricao = $"Baixa Mercado Pago: {conta.Descricao}",
            CriadoEm = DateTime.UtcNow
        });

        var contaBancaria = await db.ContasBancarias
            .Where(c => c.TenantId == cob.TenantId && c.Ativo)
            .OrderBy(c => c.CriadoEm).FirstOrDefaultAsync();

        if (contaBancaria != null)
            db.MovimentacoesBancarias.Add(new MovimentacaoBancaria
            {
                Id = Guid.NewGuid(), TenantId = cob.TenantId, ContaBancariaId = contaBancaria.Id,
                Tipo = "entrada", Valor = conta.Valor, Descricao = $"Recebido (MP) — {conta.Descricao}",
                DataMovimentacao = hoje, ContaId = conta.Id, Origem = "mercadopago",
                Conciliado = true, CriadoPor = cob.CriadoPor, CriadoEm = DateTime.UtcNow
            });
    }
}
