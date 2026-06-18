using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VetClinica.API.Services.Payments;

// ── Resultados do Mercado Pago ────────────────────────────────────────
public record MpPixResult(bool Ok, string? Erro, string? PaymentId = null,
    string? QrPayload = null, string? QrImage = null, DateTime? ExpiraEm = null);

public record MpCardResult(bool Ok, string? Erro, string? IntentId = null, string Estado = "criado");

public record MpDevice(string Id, string Name);

public record MpPagamentoInfo(bool Ok, string? Erro, string Status = "pending",
    string? ExternalReference = null, decimal Valor = 0);

/// <summary>
/// Mercado Pago — PDV integrado.
///   Pix dinâmico: POST /v1/payments (payment_method_id=pix) -> QR copia-e-cola + base64.
///   Cartão (maquininha Point): POST /point/integration-api/devices/{id}/payment-intents.
/// Confirmação de ambos via webhook (type=payment) -> GET /v1/payments/{id}.
///
/// A credencial é o Access Token da conta MP da clínica (test ou prod — o token define o
/// ambiente). Para escala, esse token vem por OAuth ("Conectar Mercado Pago"); para a v1
/// de teste, é colado nos parâmetros da clínica.
///
/// NOTA: o MP está migrando algumas rotas para a Orders API. Estes endpoints
/// (/v1/payments e /point/integration-api) são os consolidados; validar na homologação.
/// </summary>
public class MercadoPagoProvider
{
    private const string BaseUrl = "https://api.mercadopago.com";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MercadoPagoProvider> _logger;

    public string Nome => "mercadopago";

    public MercadoPagoProvider(IHttpClientFactory httpFactory, ILogger<MercadoPagoProvider> logger)
    { _httpFactory = httpFactory; _logger = logger; }

    private HttpClient Client(string accessToken)
    {
        var c = _httpFactory.CreateClient();
        c.BaseAddress = new Uri(BaseUrl);
        c.Timeout = TimeSpan.FromSeconds(25);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        return c;
    }

    public string NormalizarStatus(string statusMp) =>
        (statusMp ?? "").ToLowerInvariant() switch
        {
            "approved" or "authorized"            => "pago",
            "cancelled" or "refunded" or "charged_back" => "cancelado",
            "rejected"                            => "erro",
            _                                     => "pendente"
        };

    // ── PIX dinâmico ──────────────────────────────────────────────────
    public async Task<MpPixResult> CriarPixAsync(string accessToken, decimal valor, string descricao, string externalRef, string? notificationUrl = null, string? payerEmail = null)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return new MpPixResult(false, "Mercado Pago não configurado: informe o Access Token nos parâmetros da clínica.");

        try
        {
            using var http = Client(accessToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/payments")
            {
                Content = Json(new
                {
                    transaction_amount = valor,
                    description        = descricao,
                    payment_method_id  = "pix",
                    external_reference = externalRef,
                    notification_url   = notificationUrl,
                    payer = new { email = string.IsNullOrWhiteSpace(payerEmail) ? "consumidor.pdv@vetclinica.app" : payerEmail }
                })
            };
            req.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            var resp = await http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                return new MpPixResult(false, $"MP Pix ({(int)resp.StatusCode}): {ExtrairErro(body)}");

            var root = JsonDocument.Parse(body).RootElement;
            var paymentId = root.GetProperty("id").GetRawText(); // id é numérico
            DateTime? exp = root.TryGetProperty("date_of_expiration", out var de) && DateTime.TryParse(de.GetString(), out var d) ? d : null;

            string? payload = null, imgBase64 = null;
            if (root.TryGetProperty("point_of_interaction", out var poi)
                && poi.TryGetProperty("transaction_data", out var td))
            {
                payload   = td.TryGetProperty("qr_code", out var qc) ? qc.GetString() : null;
                imgBase64 = td.TryGetProperty("qr_code_base64", out var qb) ? qb.GetString() : null;
            }

            var imageDataUri = imgBase64 != null ? $"data:image/png;base64,{imgBase64}" : null;
            return new MpPixResult(true, null, paymentId, payload, imageDataUri, exp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MP] erro Pix: {msg}", ex.Message);
            return new MpPixResult(false, ex.Message);
        }
    }

    // ── Listar maquininhas Point da conta ─────────────────────────────
    public async Task<List<MpDevice>> ListarDispositivosAsync(string accessToken)
    {
        var lista = new List<MpDevice>();
        try
        {
            using var http = Client(accessToken);
            var resp = await http.GetAsync("/point/integration-api/devices");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return lista;

            var root = JsonDocument.Parse(body).RootElement;
            if (root.TryGetProperty("devices", out var devs))
                foreach (var dv in devs.EnumerateArray())
                    lista.Add(new MpDevice(
                        dv.GetProperty("id").GetString() ?? "",
                        dv.TryGetProperty("operating_mode", out var om) ? om.GetString() ?? "" : ""));
        }
        catch (Exception ex) { _logger.LogError(ex, "[MP] erro ao listar devices"); }
        return lista;
    }

    // ── Cartão na maquininha Point ────────────────────────────────────
    public async Task<MpCardResult> CriarOrdemCartaoAsync(string accessToken, string deviceId, decimal valor, string descricao, string externalRef)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(deviceId))
            return new MpCardResult(false, "Mercado Pago não configurado: informe Access Token e a maquininha (device).");

        try
        {
            using var http = Client(accessToken);
            // amount em CENTAVOS (inteiro) para a Point API
            var amountCents = (int)Math.Round(valor * 100m);

            using var req = new HttpRequestMessage(HttpMethod.Post, $"/point/integration-api/devices/{deviceId}/payment-intents")
            {
                Content = Json(new
                {
                    amount = amountCents,
                    description = descricao,
                    additional_info = new { external_reference = externalRef, print_on_terminal = true }
                })
            };
            req.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            var resp = await http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                return new MpCardResult(false, $"MP Point ({(int)resp.StatusCode}): {ExtrairErro(body)}");

            var root = JsonDocument.Parse(body).RootElement;
            var intentId = root.TryGetProperty("id", out var id) ? id.GetString() : null;
            var estado   = root.TryGetProperty("state", out var st) ? st.GetString() ?? "criado" : "criado";
            return new MpCardResult(true, null, intentId, estado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MP] erro cartão: {msg}", ex.Message);
            return new MpCardResult(false, ex.Message);
        }
    }

    // ── Consulta de pagamento (usado pelo webhook e pelo polling) ──────
    public async Task<MpPagamentoInfo> ConsultarPagamentoAsync(string accessToken, string paymentId)
    {
        try
        {
            using var http = Client(accessToken);
            var resp = await http.GetAsync($"/v1/payments/{paymentId}");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                return new MpPagamentoInfo(false, $"MP ({(int)resp.StatusCode}): {ExtrairErro(body)}");

            var root = JsonDocument.Parse(body).RootElement;
            var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
            var extRef = root.TryGetProperty("external_reference", out var er) ? er.GetString() : null;
            var valor  = root.TryGetProperty("transaction_amount", out var ta) ? ta.GetDecimal() : 0m;
            return new MpPagamentoInfo(true, null, NormalizarStatus(status), extRef, valor);
        }
        catch (Exception ex)
        {
            return new MpPagamentoInfo(false, ex.Message);
        }
    }

    private static StringContent Json(object o) =>
        new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    private static string ExtrairErro(string body)
    {
        try
        {
            var root = JsonDocument.Parse(body).RootElement;
            if (root.TryGetProperty("message", out var m)) return m.GetString() ?? body;
            if (root.TryGetProperty("error", out var e))   return e.GetString() ?? body;
        }
        catch { /* mantem corpo bruto */ }
        return body.Length > 300 ? body[..300] : body;
    }
}
