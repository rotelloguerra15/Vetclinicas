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
    private readonly PlatformDbContext      _platform;
    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly IConfiguration         _cfg;
    private readonly ILogger<WebhookController> _log;

    public WebhookController(
        PlatformDbContext platform,
        IServiceScopeFactory scopeFactory,
        IConfiguration cfg,
        ILogger<WebhookController> log)
    {
        _platform     = platform;
        _scopeFactory = scopeFactory;
        _cfg          = cfg;
        _log          = log;
    }

    // GET — verificação do webhook pela Meta
    [HttpGet]
    public async Task<IActionResult> Verificar(
        [FromQuery(Name = "hub.mode")]         string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")]    string? challenge)
    {
        if (mode != "subscribe" || string.IsNullOrWhiteSpace(verifyToken))
        {
            _log.LogWarning("Webhook: mode ou token ausente.");
            return Forbid();
        }

        // Busca o token esperado no banco do tenant correspondente ao phone number
        // Como a verificação vem sem phone_number_id, aceita qualquer tenant ativo
        // que tenha esse verify_token configurado
        var tenants = await _platform.Tenants
            .Where(t => t.Ativo && t.SuspensoEm == null && t.SchemaName != null)
            .ToListAsync();

        foreach (var tenant in tenants)
        {
            try
            {
                var connStr = _cfg.GetConnectionString("Default")!;
                var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TenantDbContext>()
                    .UseNpgsql(connStr).Options;
                using var db = new TenantDbContext(opts, tenant.SchemaName!);

                var botCfg = await db.BotConfigs
                    .FirstOrDefaultAsync(b => b.WebhookVerifyToken == verifyToken);

                if (botCfg != null)
                {
                    _log.LogInformation("Webhook Meta verificado para tenant {Schema}.", tenant.SchemaName);
                    return Ok(challenge);
                }
            }
            catch { /* schema pode nao ter bot_config ainda */ }
        }

        // Fallback: aceita token do appsettings (compatibilidade)
        var tokenConfig = _cfg["Meta:WebhookVerifyToken"];
        if (!string.IsNullOrWhiteSpace(tokenConfig) && verifyToken == tokenConfig)
        {
            _log.LogInformation("Webhook Meta verificado via appsettings.");
            return Ok(challenge);
        }

        _log.LogWarning("Falha na verificacao do webhook Meta. Token: {Token}", verifyToken);
        return Forbid();
    }

    // POST — recebe mensagens da Meta
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

                    var (tenantId, schemaName) = await ResolverTenant(phoneNumberId);
                    if (tenantId == null || schemaName == null)
                    {
                        _log.LogWarning("PhoneNumberId {Id} nao mapeado a nenhum tenant", phoneNumberId);
                        continue;
                    }

                    // Salva log de entrada no schema do tenant
                    var connStr = _cfg.GetConnectionString("Default")!;
                    var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TenantDbContext>()
                        .UseNpgsql(connStr).Options;
                    using var db = new TenantDbContext(opts, schemaName);

                    foreach (var msg in messages.EnumerateArray())
                    {
                        var de   = msg.GetProperty("from").GetString()!;
                        var tipo = msg.GetProperty("type").GetString();

                        string? texto = tipo switch
                        {
                            "text"   => msg.TryGetProperty("text", out var t)
                                           ? t.GetProperty("body").GetString() : null,
                            "button" => msg.TryGetProperty("button", out var b)
                                           ? b.GetProperty("payload").GetString() : null,
                            "interactive" => ExtrairInteractive(msg),
                            _ => null
                        };

                        db.BotLogs.Add(new BotLog
                        {
                            Id        = Guid.NewGuid(),
                            TenantId  = tenantId.Value,
                            Telefone  = de,
                            Direcao   = "entrada",
                            Mensagem  = texto,
                            CriadoEm  = DateTime.UtcNow
                        });
                        await db.SaveChangesAsync();

                        // Processa em novo scope
                        var tid    = tenantId.Value;
                        var schema = schemaName;
                        var tel    = de;
                        var txt    = texto;
                        var sf     = _scopeFactory;
                        var log    = _log;

                        _ = Task.Run(async () =>
                        {
                            using var scope = sf.CreateScope();
                            var bot = scope.ServiceProvider.GetRequiredService<BotWAService>();
                            try   { await bot.ProcessarMensagem(tid, tel, txt); }
                            catch (Exception ex) { log.LogError(ex, "Erro ao processar msg do bot"); }
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

    // Resolve o tenant pelo phone_number_id varrendo os schemas ativos
    private async Task<(Guid? tenantId, string? schemaName)> ResolverTenant(string? phoneNumberId)
    {
        if (string.IsNullOrEmpty(phoneNumberId)) return (null, null);

        var tenants = await _platform.Tenants
            .Where(t => t.Ativo && t.SuspensoEm == null && t.SchemaName != null)
            .ToListAsync();

        var connStr = _cfg.GetConnectionString("Default")!;

        foreach (var tenant in tenants)
        {
            try
            {
                var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TenantDbContext>()
                    .UseNpgsql(connStr).Options;
                using var db = new TenantDbContext(opts, tenant.SchemaName!);

                var cfg = await db.BotConfigs
                    .FirstOrDefaultAsync(b => b.MetaPhoneNumberId == phoneNumberId && b.Ativo);

                if (cfg != null) return (tenant.Id, tenant.SchemaName);
            }
            catch { }
        }

        return (null, null);
    }

    private static string? ExtrairInteractive(JsonElement msg)
    {
        if (!msg.TryGetProperty("interactive", out var interactive)) return null;
        var tipo = interactive.GetProperty("type").GetString();
        return tipo switch
        {
            "button_reply" => interactive.GetProperty("button_reply").GetProperty("id").GetString(),
            "list_reply"   => interactive.GetProperty("list_reply").GetProperty("id").GetString(),
            _ => null
        };
    }
}
