namespace VetClinica.API.Services;

// Integração com Z-API para envio de mensagens WhatsApp.
// Para trocar por Evolution API, basta reescrever os endpoints aqui.
public class ZApiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;
    private readonly ILogger<ZApiService> _log;

    public ZApiService(HttpClient http, IConfiguration cfg, ILogger<ZApiService> log)
    {
        _http = http;
        _cfg = cfg;
        _log = log;
    }

    private string BaseUrl()
    {
        var b = _cfg["ZApi:BaseUrl"];
        var i = _cfg["ZApi:InstanceId"];
        var t = _cfg["ZApi:Token"];
        return $"{b}/instances/{i}/token/{t}";
    }

    private async Task<bool> Post(string endpoint, object payload)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl()}/{endpoint}");
            req.Headers.Add("Client-Token", _cfg["ZApi:ClientToken"]);
            req.Content = JsonContent.Create(payload);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                _log.LogWarning("Z-API {Endpoint} falhou: {Status}", endpoint, resp.StatusCode);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Erro ao chamar Z-API {Endpoint}", endpoint);
            return false;
        }
    }

    public Task<bool> EnviarTexto(string telefone, string mensagem) =>
        Post("send-text", new { phone = LimparTelefone(telefone), message = mensagem });

    public Task<bool> EnviarImagem(string telefone, string imageUrl, string? caption = null) =>
        Post("send-image", new { phone = LimparTelefone(telefone), image = imageUrl, caption });

    // Normaliza para o formato 5531999999999
    private static string LimparTelefone(string tel)
    {
        var digits = new string(tel.Where(char.IsDigit).ToArray());
        if (digits.Length <= 11) digits = "55" + digits;
        return digits;
    }
}
