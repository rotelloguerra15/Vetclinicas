using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VetClinica.API.Services.Payments;

public record PixCobrancaResult(bool Ok, string? Erro, string? ChargeId = null,
    string? QrPayload = null, string? QrImage = null, DateTime? ExpiraEm = null);

public record PixStatusResult(bool Ok, string? Erro, string Status = "pendente"); // pendente|pago|expirado|cancelado

/// <summary>
/// Abstração de gateway de pagamento. Stateless: recebe a credencial por parâmetro
/// para servir tanto o endpoint autenticado quanto o webhook (que não tem JWT).
/// </summary>
public interface IPaymentProvider
{
    string Nome { get; }
    Task<PixCobrancaResult> CriarCobrancaPixAsync(string apiKey, string ambiente, decimal valor, string descricao, string externalRef);
    Task<PixStatusResult>   ConsultarPixAsync(string apiKey, string ambiente, string chargeId);
    /// <summary>Normaliza o status que o provider usa para o nosso padrão.</summary>
    string NormalizarStatus(string statusProvider);
}

/// <summary>
/// Asaas (https://docs.asaas.com). Sandbox e produção. Auth via header access_token.
/// Fluxo Pix: cria customer -> cria payment (billingType PIX) -> busca QR Code.
/// A confirmação chega por webhook (PAYMENT_RECEIVED / PAYMENT_CONFIRMED).
/// </summary>
public class AsaasPaymentProvider : IPaymentProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AsaasPaymentProvider> _logger;

    public string Nome => "asaas";

    public AsaasPaymentProvider(IHttpClientFactory httpFactory, ILogger<AsaasPaymentProvider> logger)
    { _httpFactory = httpFactory; _logger = logger; }

    private static string BaseUrl(string ambiente) =>
        ambiente?.Trim().ToLowerInvariant() == "producao"
            ? "https://api.asaas.com/v3"
            : "https://api-sandbox.asaas.com/v3";

    private HttpClient Client(string apiKey, string ambiente)
    {
        var c = _httpFactory.CreateClient();
        c.BaseAddress = new Uri(BaseUrl(ambiente) + "/");
        c.Timeout = TimeSpan.FromSeconds(25);
        c.DefaultRequestHeaders.Add("access_token", apiKey.Trim());
        c.DefaultRequestHeaders.UserAgent.ParseAdd("VetClinica/1.0");
        return c;
    }

    public string NormalizarStatus(string statusProvider) =>
        (statusProvider ?? "").ToUpperInvariant() switch
        {
            "RECEIVED" or "CONFIRMED" or "RECEIVED_IN_CASH" => "pago",
            "OVERDUE"                                        => "expirado",
            "REFUNDED" or "DELETED"                          => "cancelado",
            _                                                => "pendente"
        };

    public async Task<PixCobrancaResult> CriarCobrancaPixAsync(string apiKey, string ambiente, decimal valor, string descricao, string externalRef)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new PixCobrancaResult(false, "Asaas não configurado: informe a API Key nos parâmetros da clínica.");

        try
        {
            using var http = Client(apiKey, ambiente);

            // 1) Cliente (MVP: cliente genérico do PDV; otimização futura = reaproveitar por tutor)
            var custResp = await http.PostAsync("customers",
                Json(new { name = "Consumidor PDV", externalReference = externalRef }));
            var custBody = await custResp.Content.ReadAsStringAsync();
            if (!custResp.IsSuccessStatusCode)
                return new PixCobrancaResult(false, $"Asaas customer ({(int)custResp.StatusCode}): {ExtrairErro(custBody)}");
            var customerId = JsonDocument.Parse(custBody).RootElement.GetProperty("id").GetString();

            // 2) Cobrança Pix
            var pagResp = await http.PostAsync("payments", Json(new
            {
                customer          = customerId,
                billingType       = "PIX",
                value             = valor,
                dueDate           = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                description       = descricao,
                externalReference = externalRef
            }));
            var pagBody = await pagResp.Content.ReadAsStringAsync();
            if (!pagResp.IsSuccessStatusCode)
                return new PixCobrancaResult(false, $"Asaas payment ({(int)pagResp.StatusCode}): {ExtrairErro(pagBody)}");
            var paymentId = JsonDocument.Parse(pagBody).RootElement.GetProperty("id").GetString();

            // 3) QR Code Pix
            var qrResp = await http.GetAsync($"payments/{paymentId}/pixQrCode");
            var qrBody = await qrResp.Content.ReadAsStringAsync();
            if (!qrResp.IsSuccessStatusCode)
                return new PixCobrancaResult(false, $"Asaas pixQrCode ({(int)qrResp.StatusCode}): {ExtrairErro(qrBody)}");

            var qr = JsonDocument.Parse(qrBody).RootElement;
            var payload   = qr.TryGetProperty("payload", out var p) ? p.GetString() : null;
            var encoded   = qr.TryGetProperty("encodedImage", out var e) ? e.GetString() : null;
            DateTime? exp = qr.TryGetProperty("expirationDate", out var ed) && DateTime.TryParse(ed.GetString(), out var d) ? d : null;

            var imageDataUri = encoded != null ? $"data:image/png;base64,{encoded}" : null;

            _logger.LogInformation("[Asaas] Pix criado payment={id} ref={ref}", paymentId, externalRef);
            return new PixCobrancaResult(true, null, paymentId, payload, imageDataUri, exp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Asaas] erro ao criar Pix: {msg}", ex.Message);
            return new PixCobrancaResult(false, ex.Message);
        }
    }

    public async Task<PixStatusResult> ConsultarPixAsync(string apiKey, string ambiente, string chargeId)
    {
        try
        {
            using var http = Client(apiKey, ambiente);
            var resp = await http.GetAsync($"payments/{chargeId}");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                return new PixStatusResult(false, $"Asaas ({(int)resp.StatusCode}): {ExtrairErro(body)}");

            var status = JsonDocument.Parse(body).RootElement.GetProperty("status").GetString() ?? "";
            return new PixStatusResult(true, null, NormalizarStatus(status));
        }
        catch (Exception ex)
        {
            return new PixStatusResult(false, ex.Message);
        }
    }

    private static StringContent Json(object o) =>
        new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    private static string ExtrairErro(string body)
    {
        try
        {
            var errs = JsonDocument.Parse(body).RootElement.GetProperty("errors");
            if (errs.GetArrayLength() > 0)
                return errs[0].GetProperty("description").GetString() ?? body;
        }
        catch { /* mantem corpo bruto */ }
        return body.Length > 300 ? body[..300] : body;
    }
}
