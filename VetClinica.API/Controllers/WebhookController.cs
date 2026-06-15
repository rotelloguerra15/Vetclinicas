using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VetClinica.API.Data;
using VetClinica.API.Models;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

/// <summary>
/// Webhook da Meta Cloud API.
///
/// GET  /api/bot/webhook  — verificação inicial do Meta (challenge)
/// POST /api/bot/webhook  — recebe mensagens
///
/// No painel Meta for Developers:
///   URL:          https://vetclinicas-production.up.railway.app/api/bot/webhook
///   Verify Token: (valor da variável Meta__WebhookVerifyToken no Railway)
///   Campos:       messages
///
/// Diferente da Z-API, o Meta envia para uma URL única (não por tenantId).
/// O tenantId é resolvido pelo número do destinatário (To) cruzando com bot_config.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/bot/webhook")]
public class WebhookController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly BotWAService _bot;
    private readonly IConfiguration _cfg;
    private readonly ILogger<WebhookController> _log;

    public WebhookController(AppDbContext db, BotWAService bot,
        IConfiguration cfg, ILogger<WebhookController> log)
    { _db = db; _bot = bot; _cfg = cfg; _log = log; }

    // ── GET: verificação do Meta ─────────────────────────────────────────────
    [HttpGet]
    public IActionResult Verificar(
        [FromQuery(Name = "hub.mode")]         string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")]    string? challenge)
    {
        var tokenEsperado = _cfg["Meta:WebhookVerifyToken"];
        if (mode == "subscribe" && verifyToken == tokenEsperado)
        {
            _log.LogInformation("Webhook Meta verificado com sucesso.");
            return Ok(challenge);
        }
        _log.LogWarning("Falha na verificação do webhook Meta. Token recebido: {Token}", verifyToken);
        return Forbid();
    }

    // ── POST: recebe mensagens ───────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ReceberMensagem([FromBody] JsonElement payload)
    {
        try
        {
            // Estrutura Meta:
            // { "object": "whatsapp_business_account",
            //   "entry": [{ "id": "WABA_ID", "changes": [{ "value": {
            //     "metadata": { "phone_number_id": "..." },
            //     "messages": [{ "from": "5531...", "type": "text",
            //                    "text": { "body": "oi" } }]
            //   }}]}]}

            if (!payload.TryGetProperty("object", out var objProp) ||
                objProp.GetString() != "whatsapp_business_account")
                return Ok(); // não é mensagem WA

            var entries = payload.GetProperty("entry");
            foreach (var entry in entries.EnumerateArray())
            {
                foreach (var change in entry.GetProperty("changes").EnumerateArray())
                {
                    var value = change.GetProperty("value");

                    // Obter o Phone Number ID para resolver o tenantId
                    var phoneNumberId = value
                        .GetProperty("metadata")
                        .GetProperty("phone_number_id")
                        .GetString();

                    // Ignorar se não tiver mensagens (pode ser status update)
                    if (!value.TryGetProperty("messages", out var messages)) continue;

                    // Resolver tenantId pelo phoneNumberId configurado
                    var tenantId = await ResolverTenantId(phoneNumberId);
                    if (tenantId == null)
                    {
                        _log.LogWarning("PhoneNumberId {Id} nao mapeado a nenhum tenant", phoneNumberId);
                        continue;
                    }

                    foreach (var msg in messages.EnumerateArray())
                    {
                        var de   = msg.GetProperty("from").GetString()!;
                        var tipo = msg.GetProperty("type").GetString();

                        // Extrair texto conforme tipo
                        string? texto = tipo switch
                        {
                            "text"         => msg.TryGetProperty("text", out var t)
                                                ? t.GetProperty("body").GetString() : null,
                            "button"       => msg.TryGetProperty("button", out var b)
                                                ? b.GetProperty("payload").GetString() : null,
                            "interactive"  => ExtrairInteractive(msg),
                            _              => null
                        };

                        if (string.IsNullOrWhiteSpace(texto)) continue;

                        // Log de entrada
                        _db.BotLogs.Add(new BotLog
                        {
                            Id       = Guid.NewGuid(),
                            TenantId = tenantId.Value,
                            Telefone = de,
                            Direcao  = "entrada",
                            Mensagem = texto,
                            CriadoEm = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync();

                        // Processar (fire and forget)
                        var tid  = tenantId.Value;
                        var tel  = de;
                        var txt  = texto;
                        _ = Task.Run(async () =>
                        {
                            try { await _bot.ProcessarMensagem(tid, tel, txt); }
                            catch (Exception ex)
                            { _log.LogError(ex, "Erro ao processar msg do bot"); }
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Erro no webhook Meta");
        }

        // Meta exige sempre 200
        return Ok();
    }

    // ── Resolve tenantId a partir do phoneNumberId ───────────────────────────
    // Armazena o phoneNumberId no bot_config (campo adicionado na migration 040)
    private async Task<Guid?> ResolverTenantId(string? phoneNumberId)
    {
        if (string.IsNullOrEmpty(phoneNumberId)) return null;

        var cfg = await _db.BotConfigs
            .FirstOrDefaultAsync(b => b.MetaPhoneNumberId == phoneNumberId);

        return cfg?.TenantId;
    }

    // ── Extrai texto de mensagens interativas (list/button reply) ────────────
    private static string? ExtrairInteractive(JsonElement msg)
    {
        if (!msg.TryGetProperty("interactive", out var interactive)) return null;

        var tipo = interactive.GetProperty("type").GetString();
        return tipo switch
        {
            "button_reply" => interactive.GetProperty("button_reply")
                                         .GetProperty("id").GetString(),
            "list_reply"   => interactive.GetProperty("list_reply")
                                         .GetProperty("id").GetString(),
            _              => null
        };
    }
}
