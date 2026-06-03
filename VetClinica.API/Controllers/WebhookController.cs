using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VetClinica.API.Data;
using VetClinica.API.Models;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

/// <summary>
/// Recebe mensagens do WhatsApp via Z-API webhook.
/// URL: POST /api/webhook/whatsapp/{tenantId}
/// Configurar na Z-API: Webhook de mensagens recebidas apontando para esta URL.
/// </summary>
[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly BotWhatsAppService _bot;
    private readonly IConfiguration _cfg;
    private readonly ILogger<WebhookController> _log;

    public WebhookController(AppDbContext db, BotWhatsAppService bot,
        IConfiguration cfg, ILogger<WebhookController> log)
    { _db = db; _bot = bot; _cfg = cfg; _log = log; }

    [HttpPost("whatsapp/{tenantId}")]
    public async Task<IActionResult> ReceberMensagem(Guid tenantId, [FromBody] JsonElement payload)
    {
        try
        {
            // Valida secret opcional (configurado em Bot:WebhookSecret no appsettings)
            var secret = _cfg["Bot:WebhookSecret"];
            if (!string.IsNullOrEmpty(secret))
            {
                Request.Headers.TryGetValue("X-Webhook-Secret", out var headerSecret);
                if (headerSecret.ToString() != secret)
                    return Unauthorized();
            }

            // Extrai telefone e texto do payload da Z-API
            // Formato Z-API: { "phone": "5531999999999", "text": { "message": "oi" }, "isGroup": false }
            if (!payload.TryGetProperty("phone", out var phoneProp)) return Ok();
            if (!payload.TryGetProperty("isGroup", out var isGroup)) return Ok();
            if (isGroup.GetBoolean()) return Ok(); // ignora grupos

            var telefone = phoneProp.GetString();
            if (string.IsNullOrEmpty(telefone)) return Ok();

            // Extrai texto — pode vir em diferentes campos dependendo do tipo de mensagem
            string? texto = null;
            if (payload.TryGetProperty("text", out var textObj))
            {
                if (textObj.ValueKind == JsonValueKind.Object &&
                    textObj.TryGetProperty("message", out var msgProp))
                    texto = msgProp.GetString();
                else if (textObj.ValueKind == JsonValueKind.String)
                    texto = textObj.GetString();
            }
            // Botoes interativos Z-API
            if (texto == null && payload.TryGetProperty("buttonResponse", out var btnResp))
            {
                if (btnResp.TryGetProperty("buttonId", out var btnId))
                    texto = btnId.GetString();
            }

            if (string.IsNullOrEmpty(texto)) return Ok(); // ignora midia, sticker, etc.

            // Salva log de entrada
            _db.BotLogs.Add(new BotLog
            {
                Id       = Guid.NewGuid(),
                TenantId = tenantId,
                Telefone = telefone,
                Direcao  = "entrada",
                Mensagem = texto,
                CriadoEm = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            // Processa no bot (fire and forget com tratamento de erro)
            _ = Task.Run(async () =>
            {
                try { await _bot.ProcessarMensagem(tenantId, telefone, texto); }
                catch (Exception ex) { _log.LogError(ex, "Erro ao processar mensagem do bot"); }
            });

            return Ok(new { recebido = true });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Erro no webhook WhatsApp");
            return Ok(); // sempre retorna 200 para Z-API nao retentar
        }
    }
}
