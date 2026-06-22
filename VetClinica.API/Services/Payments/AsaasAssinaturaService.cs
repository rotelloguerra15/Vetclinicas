using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services.Payments;

public record AssinaturaResult(bool Ok, string? Erro, string? AssinaturaId = null,
    string? ClienteId = null, string? InvoiceUrl = null);

/// <summary>
/// Assinatura SaaS da Ketra (cobra cada CLINICA pelo uso da plataforma).
/// Conta Asaas DA PLATAFORMA -- diferente de AsaasPaymentProvider, que usa a
/// conta Asaas de CADA CLINICA pra receber Pix no PDV deles.
///
/// Chaves em platform.configuracoes:
///   asaas_saas_api_key       -> API Key da conta Asaas da Ketra
///   asaas_saas_ambiente      -> "sandbox" (padrao) | "producao"
///   asaas_saas_webhook_token -> opcional; se configurado, o webhook so aceita
///                                requisicoes com o header "asaas-access-token" igual
///
/// billingType = "UNDEFINED": o cliente escolhe cartao/boleto/Pix na pagina
/// hospedada do proprio Asaas (invoiceUrl). Evita a gente coletar dado de
/// cartao (escopo de PCI-DSS).
///
/// ATENCAO: nunca testado contra a API real do Asaas (sem credenciais ainda).
/// Segue exatamente o mesmo padrao do AsaasPaymentProvider (que ja funciona
/// em produção para Pix do PDV) -- mas testar em sandbox antes de produção.
/// </summary>
public class AsaasAssinaturaService
{
    private readonly PlatformDbContext _platform;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AsaasAssinaturaService> _logger;

    public AsaasAssinaturaService(PlatformDbContext platform, IHttpClientFactory httpFactory, ILogger<AsaasAssinaturaService> logger)
    { _platform = platform; _httpFactory = httpFactory; _logger = logger; }

    private async Task<string?> Cfg(string chave) =>
        (await _platform.Configuracoes.FirstOrDefaultAsync(c => c.Chave == chave))?.Valor;

    private static string BaseUrl(string ambiente) =>
        ambiente?.Trim().ToLowerInvariant() == "producao"
            ? "https://api.asaas.com/v3"
            : "https://api-sandbox.asaas.com/v3";

    public async Task<AssinaturaResult> CriarAssinatura(Tenant tenant, decimal valor, string planoId, string descricao)
    {
        var apiKey   = await Cfg("asaas_saas_api_key");
        var ambiente = await Cfg("asaas_saas_ambiente") ?? "sandbox";

        if (string.IsNullOrWhiteSpace(apiKey))
            return new AssinaturaResult(false, "Asaas (assinatura SaaS) nao configurado. Configure a API Key da Ketra em Configuracoes no Painel da Plataforma.");

        try
        {
            using var http = _httpFactory.CreateClient();
            http.BaseAddress = new Uri(BaseUrl(ambiente) + "/");
            http.Timeout = TimeSpan.FromSeconds(25);
            http.DefaultRequestHeaders.Add("access_token", apiKey.Trim());
            http.DefaultRequestHeaders.UserAgent.ParseAdd("VetClinica/1.0");

            // 1) Reaproveita o cliente Asaas se a clinica ja tiver um; senao cria.
            var clienteId = tenant.AsaasClienteId;
            if (string.IsNullOrWhiteSpace(clienteId))
            {
                var custResp = await http.PostAsync("customers", Json(new
                {
                    name              = tenant.Nome,
                    email             = tenant.Email,
                    externalReference = tenant.Id.ToString()
                }));
                var custBody = await custResp.Content.ReadAsStringAsync();
                if (!custResp.IsSuccessStatusCode)
                    return new AssinaturaResult(false, $"Asaas customer ({(int)custResp.StatusCode}): {ExtrairErro(custBody)}");
                clienteId = JsonDocument.Parse(custBody).RootElement.GetProperty("id").GetString();
            }

            // 2) Cria a assinatura recorrente. externalReference leva "{tenantId}|{planoId}"
            // pro webhook saber pra qual plano promover a clinica quando o 1o pagamento confirmar.
            var subResp = await http.PostAsync("subscriptions", Json(new
            {
                customer          = clienteId,
                billingType       = "UNDEFINED",   // cliente escolhe cartao/boleto/Pix na pagina do Asaas
                value             = valor,
                nextDueDate       = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                cycle             = "MONTHLY",
                description       = descricao,
                externalReference = $"{tenant.Id}|{planoId}"
            }));
            var subBody = await subResp.Content.ReadAsStringAsync();
            if (!subResp.IsSuccessStatusCode)
                return new AssinaturaResult(false, $"Asaas subscription ({(int)subResp.StatusCode}): {ExtrairErro(subBody)}", clienteId: clienteId);
            var assinaturaId = JsonDocument.Parse(subBody).RootElement.GetProperty("id").GetString();

            // 3) Busca o link de pagamento (invoiceUrl) do 1o pagamento gerado pela assinatura.
            // O Asaas gera esse pagamento de forma assíncrona -- pode nao estar disponivel
            // ainda na mesma chamada. Se vier vazio, a assinatura ja foi criada (assinaturaId
            // preenchido); o link pode ser conferido depois direto no painel do Asaas.
            string? invoiceUrl = null;
            try
            {
                var paysResp = await http.GetAsync($"payments?subscription={assinaturaId}");
                if (paysResp.IsSuccessStatusCode)
                {
                    var paysBody = await paysResp.Content.ReadAsStringAsync();
                    var data = JsonDocument.Parse(paysBody).RootElement.GetProperty("data");
                    if (data.GetArrayLength() > 0 && data[0].TryGetProperty("invoiceUrl", out var iu))
                        invoiceUrl = iu.GetString();
                }
            }
            catch { /* nao impede o retorno -- assinatura ja foi criada */ }

            _logger.LogInformation("[AsaasAssinatura] Criada assinatura={id} tenant={t} plano={p}", assinaturaId, tenant.Id, planoId);
            return new AssinaturaResult(true, null, assinaturaId, clienteId, invoiceUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AsaasAssinatura] erro ao criar assinatura para tenant {t}", tenant.Id);
            return new AssinaturaResult(false, ex.Message);
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
        catch { }
        return body.Length > 300 ? body[..300] : body;
    }
}
