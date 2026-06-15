using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VetClinica.API.Data;
using VetClinica.API.Models;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/bot/webhook")]
public class WebhookController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _cfg;
    private readonly ILogger<WebhookController> _log;

    public WebhookController(AppDbContext db, IServiceScopeFactory scopeFactory,
        IConfiguration cfg, ILogger<WebhookController> log)
    { _db = db; _scopeFactory = scopeFactory; _cfg = cfg; _log = log; }

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
        _log.LogWarning("Falha na verificacao do webhook Meta. Token recebido: {Token}", verifyToken);
        return Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> ReceberMensagem([FromBody] JsonElement payload)
    {
        try
        {
            if (!payload.TryGetProperty("object", out var objProp) ||
                objProp.GetString() != "whatsapp_business_account")
                return Ok();

            var entries = payload.GetProperty("entry");
            foreach (var entry in entries.EnumerateArray())
            {
                foreach (var change in entry.GetProperty("changes").EnumerateArray())
                {
                    var value = change.GetProperty("value");

                    var phoneNumberId = value
                        .GetProperty("metadata")
                        .GetProperty("phone_number_id")
                        .GetString();

                    if (!value.TryGetProperty("messages", out var messages)) continue;

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

                        string? texto = tipo switch
                        {
                            "text"        => msg.TryGetProperty("text", out var t)
                                               ? t.GetProperty("body").GetString() : null,
                            "button"      => msg.TryGetProperty("button", out var b)
                                               ? b.GetProperty("payload").GetString() : null,
                            "interactive" => ExtrairInteractive(msg),
                            _             => null
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

                        // Processar em novo scope (evita DbContext disposed)
                        var tid = tenantId.Value;
                        var tel = de;
                        var txt = texto;
                        var sf  = _scopeFactory;
                        var log = _log;

                        _ = Task.Run(async () =>
                        {
                            using var scope = sf.CreateScope();
                            var bot = scope.ServiceProvider.GetRequiredService<BotWAService>();
                            try
                            {
                                await bot.ProcessarMensagem(tid, tel, txt);
                            }
                            catch (Exception ex)
                            {
                                log.LogError(ex, "Erro ao processar msg do bot");
                            }
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Erro no webhook Meta");
        }

        return Ok();
    }

    private async Task<Guid?> ResolverTenantId(string? phoneNumberId)
    {
        if (string.IsNullOrEmpty(phoneNumberId)) return null;
        var cfg = await _db.BotConfigs
            .FirstOrDefaultAsync(b => b.MetaPhoneNumberId == phoneNumberId);
        return cfg?.TenantId;
    }

    private static string? ExtrairInteractive(JsonElement msg)
    {
        if (!msg.TryGetProperty("interactive", out var interactive)) return null;
        var tipo = interactive.GetProperty("type").GetString();
        return tipo switch
        {
            "button_reply" => interactive.GetProperty("button_reply").GetProperty("id").GetString(),
            "list_reply"   => interactive.GetProperty("list_reply").GetProperty("id").GetString(),
            _              => null
        };
    }
}
