using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;

namespace VetClinica.API.Services;

/// <summary>
/// Resultado de um envio de email.
/// </summary>
public record EmailResult(bool Ok, string? Erro = null, string? Provider = null, string? Id = null);

public interface IEmailService
{
    /// <summary>
    /// Envia um email HTML usando o provider configurado em platform.configuracoes
    /// (email_provider = "resend" ou "smtp"). Padrao: resend.
    /// </summary>
    Task<EmailResult> EnviarAsync(string para, string assunto, string htmlBody);
}

/// <summary>
/// Servico central de email. Le a configuracao de platform.configuracoes e despacha
/// pelo provider ativo.
///
/// Por que Resend e o padrao: o Railway bloqueia TODAS as portas SMTP de saida
/// (25, 465, 587, 2525) nos planos Free/Trial/Hobby. O Resend envia via REST sobre
/// a porta 443 (HTTPS), que nunca e bloqueada. O caminho SMTP fica disponivel para
/// quando o projeto migrar para o plano Pro ou rodar local.
///
/// Chaves em platform.configuracoes:
///   email_provider    -> "resend" (padrao) | "smtp"
///   resend_api_key    -> chave re_xxxxx do Resend
///   resend_remetente  -> ex: "VetClinica by Ketra &lt;nao-responda@ketra.com.br&gt;"
///                        (precisa de dominio verificado no Resend; para teste use
///                         "VetClinica &lt;onboarding@resend.dev&gt;")
///   smtp_host / smtp_porta / smtp_usuario / smtp_senha / smtp_ssl / smtp_remetente
/// </summary>
public class EmailService : IEmailService
{
    private readonly PlatformDbContext   _platform;
    private readonly IHttpClientFactory  _httpFactory;
    private readonly ILogger<EmailService> _logger;

    private const string ResendEndpoint = "https://api.resend.com/emails";
    private const string ResendRemetentePadrao = "VetClinica by Ketra <onboarding@resend.dev>";

    public EmailService(PlatformDbContext platform, IHttpClientFactory httpFactory, ILogger<EmailService> logger)
    {
        _platform    = platform;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    private async Task<string?> Cfg(string chave) =>
        (await _platform.Configuracoes.AsNoTracking().FirstOrDefaultAsync(c => c.Chave == chave))?.Valor;

    public async Task<EmailResult> EnviarAsync(string para, string assunto, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(para))
            return new EmailResult(false, "Destinatario vazio.");

        var provider = (await Cfg("email_provider") ?? "resend").Trim().ToLowerInvariant();

        return provider == "smtp"
            ? await EnviarSmtpAsync(para, assunto, htmlBody)
            : await EnviarResendAsync(para, assunto, htmlBody);
    }

    // ── Resend (HTTP / porta 443) ─────────────────────────────────────────
    private async Task<EmailResult> EnviarResendAsync(string para, string assunto, string htmlBody)
    {
        var apiKey    = await Cfg("resend_api_key");
        var remetente = await Cfg("resend_remetente");

        if (string.IsNullOrWhiteSpace(remetente))
            remetente = ResendRemetentePadrao;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("[Email] Resend sem api_key configurada.");
            return new EmailResult(false, "Resend nao configurado: informe a API Key no painel admin.", "resend");
        }

        try
        {
            var payload = new
            {
                from    = remetente,
                to      = new[] { para },
                subject = assunto,
                html    = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);

            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, ResendEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            reqMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            var resp = await client.SendAsync(reqMsg);
            var corpo = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
            {
                string? id = null;
                try { id = JsonDocument.Parse(corpo).RootElement.GetProperty("id").GetString(); }
                catch { /* resposta sem id explicito */ }

                _logger.LogInformation("[Email] Resend OK para {para} (id={id})", para, id);
                return new EmailResult(true, null, "resend", id);
            }

            // Extrai a mensagem de erro da Resend para facilitar o diagnostico
            var erro = corpo;
            try { erro = JsonDocument.Parse(corpo).RootElement.GetProperty("message").GetString() ?? corpo; }
            catch { /* mantem corpo bruto */ }

            _logger.LogError("[Email] Resend falhou ({status}): {erro}", (int)resp.StatusCode, erro);
            return new EmailResult(false, $"Resend ({(int)resp.StatusCode}): {erro}", "resend");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Resend excecao: {msg}", ex.Message);
            return new EmailResult(false, ex.Message, "resend");
        }
    }

    // ── SMTP (somente plano Pro do Railway ou ambiente local) ─────────────
    private async Task<EmailResult> EnviarSmtpAsync(string para, string assunto, string htmlBody)
    {
        var host      = await Cfg("smtp_host");
        var portaTxt  = await Cfg("smtp_porta") ?? "587";
        var usuario   = await Cfg("smtp_usuario");
        var senha     = await Cfg("smtp_senha");
        var sslTxt    = await Cfg("smtp_ssl") ?? "true";
        var remetente = await Cfg("smtp_remetente") ?? usuario;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(usuario))
            return new EmailResult(false, "SMTP nao configurado: preencha host e usuario.", "smtp");

        if (!int.TryParse(portaTxt, out var porta)) porta = 587;
        if (!bool.TryParse(sslTxt, out var ssl))    ssl   = true;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var client = new SmtpClient(host, porta)
            {
                EnableSsl      = ssl,
                Credentials    = new NetworkCredential(usuario, senha),
                Timeout        = 15000,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var msg = new MailMessage
            {
                From       = new MailAddress(remetente!, "VetClinica by Ketra"),
                Subject    = assunto,
                IsBodyHtml = true,
                Body       = htmlBody
            };
            msg.To.Add(para);

            await client.SendMailAsync(msg, cts.Token);
            _logger.LogInformation("[Email] SMTP OK para {para}", para);
            return new EmailResult(true, null, "smtp");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] SMTP excecao: {msg}", ex.Message);
            var detalhe = ex.InnerException?.Message ?? ex.Message;
            return new EmailResult(false, detalhe, "smtp");
        }
    }
}
