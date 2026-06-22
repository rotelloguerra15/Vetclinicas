using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VetClinica.API.Data;

namespace VetClinica.API.Controllers;

// Webhook separado do PixController (que é por-clinica, conta Asaas de cada
// clinica). Este é nivel plataforma: conta Asaas da Ketra, atualiza
// platform.tenants direto (sem schema de clinica nenhum envolvido).
[ApiController]
[Route("api/webhooks/asaas-assinatura")]
public class AsaasAssinaturaWebhookController : ControllerBase
{
    private readonly PlatformDbContext _platform;
    private readonly ILogger<AsaasAssinaturaWebhookController> _logger;

    public AsaasAssinaturaWebhookController(PlatformDbContext platform, ILogger<AsaasAssinaturaWebhookController> logger)
    { _platform = platform; _logger = logger; }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        try
        {
            // Se um token de webhook foi configurado, exige o header do Asaas.
            var tokenEsperado = (await _platform.Configuracoes
                .FirstOrDefaultAsync(c => c.Chave == "asaas_saas_webhook_token"))?.Valor;
            if (!string.IsNullOrWhiteSpace(tokenEsperado))
            {
                var tokenRecebido = Request.Headers["asaas-access-token"].FirstOrDefault();
                if (tokenRecebido != tokenEsperado)
                {
                    _logger.LogWarning("[AsaasAssinatura] webhook com token invalido");
                    return Ok(new { ignored = true }); // 200 para o Asaas nao reenfileirar
                }
            }

            var root = JsonDocument.Parse(raw).RootElement;
            var evento = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;
            if (!root.TryGetProperty("payment", out var pay))
                return Ok(new { ignored = true });

            var externalRef = pay.TryGetProperty("externalReference", out var er) ? er.GetString() : null;
            if (string.IsNullOrWhiteSpace(externalRef) || !externalRef.Contains('|'))
                return Ok(new { ignored = true, motivo = "sem externalReference" });

            // externalRef = "{tenantId}|{planoId}"
            var partes = externalRef.Split('|');
            if (!Guid.TryParse(partes[0], out var tenantId))
                return Ok(new { ignored = true });
            var planoId = partes.Length > 1 ? partes[1] : null;

            var tenant = await _platform.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null) return Ok(new { ignored = true, motivo = "tenant nao encontrado" });

            switch (evento)
            {
                case "PAYMENT_CONFIRMED":
                case "PAYMENT_RECEIVED":
                    tenant.StatusPagamento = "pago";
                    tenant.SuspensoEm = null; // reativa se estava suspensa por trial vencido / atraso
                    tenant.UltimoAvisoAtrasoEm = null;
                    if (!string.IsNullOrWhiteSpace(planoId)) tenant.Plano = planoId;
                    if (pay.TryGetProperty("nextDueDate", out var ndd) && DateTime.TryParse(ndd.GetString(), out var prox))
                        tenant.ProximoFaturamento = prox;
                    _logger.LogInformation("[AsaasAssinatura] pagamento confirmado tenant={t} evento={e}", tenantId, evento);
                    break;

                case "PAYMENT_OVERDUE":
                    // So marca o status -- NAO suspende automaticamente aqui.
                    // Suspender clinica pagante em atraso é decisao de negocio (regra de carencia,
                    // quantos dias, aviso previo etc.) que ainda nao foi definida.
                    tenant.StatusPagamento = "atraso";
                    _logger.LogWarning("[AsaasAssinatura] pagamento em atraso tenant={t}", tenantId);
                    break;

                default:
                    return Ok(new { ignored = true, evento });
            }

            await _platform.SaveChangesAsync();
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AsaasAssinatura] webhook erro: {msg}", ex.Message);
            // 200 mesmo no erro -- evita o Asaas reenfileirar infinitamente payload malformado
            return Ok(new { ok = false });
        }
    }
}
