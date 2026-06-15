using System.Net.Http.Headers;
using System.Text.Json;

namespace VetClinica.API.Services;

/// <summary>
/// Integração com Meta Cloud API (WhatsApp Business).
/// Substitui ZApiService. Configurar no Railway:
///   Meta__PhoneNumberId   = ID do número no Meta
///   Meta__Token           = Token permanente do System User
///   Meta__WabaId          = WhatsApp Business Account ID
/// </summary>
public class WhatsAppService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;
    private readonly ILogger<WhatsAppService> _log;

    private const string BaseUrl = "https://graph.facebook.com/v19.0";

    public WhatsAppService(HttpClient http, IConfiguration cfg, ILogger<WhatsAppService> log)
    {
        _http = http;
        _cfg  = cfg;
        _log  = log;
    }

    private string PhoneId  => _cfg["Meta:PhoneNumberId"] ?? throw new InvalidOperationException("Meta:PhoneNumberId nao configurado");
    private string Token    => _cfg["Meta:Token"]         ?? throw new InvalidOperationException("Meta:Token nao configurado");

    // ── Envio de texto livre ─────────────────────────────────────────────────
    // Usado dentro de conversas ativas (janela de 24h após última msg do cliente)
    public Task<bool> EnviarTexto(string telefone, string mensagem) =>
        Post(new
        {
            messaging_product = "whatsapp",
            to                = LimparTelefone(telefone),
            type              = "text",
            text              = new { preview_url = false, body = mensagem }
        });

    // ── Envio de imagem com legenda ──────────────────────────────────────────
    public Task<bool> EnviarImagem(string telefone, string imageUrl, string? caption = null) =>
        Post(new
        {
            messaging_product = "whatsapp",
            to                = LimparTelefone(telefone),
            type              = "image",
            image             = new { link = imageUrl, caption = caption ?? "" }
        });

    // ── Templates (mensagens proativas fora da janela de 24h) ────────────────

    /// <summary>Lembrete de agendamento — template: lembrete_agendamento</summary>
    /// Parâmetros: tutorNome, petNome, horario (HH:mm), clinicaNome
    public Task<bool> EnviarLembreteAgendamento(
        string telefone, string tutorNome, string petNome,
        string horario, string clinicaNome) =>
        EnviarTemplate(telefone, "lembrete_agendamento",
            tutorNome, petNome, horario, clinicaNome);

    /// <summary>Pet pronto para retirada — template: pet_pronto</summary>
    /// Parâmetros: tutorNome, petNome, clinicaNome
    public Task<bool> EnviarPetPronto(
        string telefone, string tutorNome, string petNome, string clinicaNome) =>
        EnviarTemplate(telefone, "pet_pronto", tutorNome, petNome, clinicaNome);

    /// <summary>Promoção/marketing — template: promocao_marketing</summary>
    /// Parâmetros: tutorNome, textoCampanha, linkAgendamento
    public Task<bool> EnviarPromocao(
        string telefone, string tutorNome, string textoCampanha, string linkAgendamento) =>
        EnviarTemplate(telefone, "promocao_marketing", tutorNome, textoCampanha, linkAgendamento);

    // ── Core: envia template aprovado pelo Meta ──────────────────────────────
    private Task<bool> EnviarTemplate(string telefone, string templateName, params string[] parametros)
    {
        var components = parametros.Length == 0
            ? Array.Empty<object>()
            : new object[]
            {
                new
                {
                    type       = "body",
                    parameters = parametros.Select(p => new { type = "text", text = p }).ToArray()
                }
            };

        return Post(new
        {
            messaging_product = "whatsapp",
            to                = LimparTelefone(telefone),
            type              = "template",
            template          = new
            {
                name     = templateName,
                language = new { code = "pt_BR" },
                components
            }
        });
    }

    // ── HTTP ─────────────────────────────────────────────────────────────────
    private async Task<bool> Post(object payload)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{PhoneId}/messages");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            req.Content = JsonContent.Create(payload);

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _log.LogWarning("Meta API falhou {Status}: {Body}", resp.StatusCode, body);
            }
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Erro ao chamar Meta Cloud API");
            return false;
        }
    }

    // ── Normaliza para formato internacional sem + ───────────────────────────
    public static string LimparTelefone(string tel)
    {
        var digits = new string(tel.Where(char.IsDigit).ToArray());
        if (digits.Length <= 11) digits = "55" + digits;
        return digits;
    }
}
