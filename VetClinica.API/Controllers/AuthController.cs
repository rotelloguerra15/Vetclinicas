using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService       _auth;
    private readonly PlatformDbContext _platform;
    private readonly IConfiguration    _cfg;

    public AuthController(AuthService auth, PlatformDbContext platform, IConfiguration cfg)
    {
        _auth     = auth;
        _platform = platform;
        _cfg      = cfg;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var result = await _auth.Login(req);
        if (result == null) return Unauthorized(new { erro = "Credenciais invalidas" });
        return Ok(result);
    }

    [HttpPost("login-plataforma")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginPlataforma(LoginRequest req)
    {
        var result = await _auth.LoginPlataforma(req);
        if (result == null) return Unauthorized(new { erro = "Credenciais invalidas" });
        return Ok(result);
    }

    // ────────────────────────────────────────────────────────────────
    // ESQUECI MINHA SENHA
    // POST /api/auth/esqueci-senha  { email }
    // ────────────────────────────────────────────────────────────────
    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { erro = "Informe o e-mail." });

        var tenant = await _platform.Tenants
            .FirstOrDefaultAsync(t => t.Email == req.Email.Trim().ToLower() && t.Ativo);

        // Responde sempre OK para nao revelar se email existe
        if (tenant == null)
            return Ok(new { mensagem = "Se o e-mail existir, voce recebera as instrucoes." });

        // Gera token unico (40 chars hex)
        var token  = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(20)).ToLower();
        var expira = DateTime.UtcNow.AddMinutes(30);

        tenant.TokenResetSenha  = token;
        tenant.TokenResetExpira = expira;
        await _platform.SaveChangesAsync();

        var frontendUrl = _cfg["App:FrontendUrl"] ?? "https://vetclinicas.vercel.app";
        var linkReset   = $"{frontendUrl}/redefinir-senha?token={token}";

        // Envia email em background
        _ = Task.Run(async () =>
        {
            try { await EnviarEmailReset(req.Email.Trim().ToLower(), tenant.Nome, linkReset); }
            catch (Exception ex) { Console.WriteLine($"[Reset] Email erro: {ex.Message}"); }
        });

        // Tenta WhatsApp se tiver numero e bot configurado
        if (!string.IsNullOrWhiteSpace(tenant.Telefone) && !string.IsNullOrWhiteSpace(tenant.SchemaName))
        {
            _ = Task.Run(async () =>
            {
                try { await EnviarWhatsAppReset(tenant, linkReset); }
                catch (Exception ex) { Console.WriteLine($"[Reset] WhatsApp erro: {ex.Message}"); }
            });
        }

        return Ok(new { mensagem = "Se o e-mail existir, voce recebera as instrucoes." });
    }

    // ────────────────────────────────────────────────────────────────
    // REDEFINIR SENHA
    // POST /api/auth/redefinir-senha  { token, novaSenha }
    // ────────────────────────────────────────────────────────────────
    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NovaSenha))
            return BadRequest(new { erro = "Token e nova senha sao obrigatorios." });

        if (req.NovaSenha.Length < 6)
            return BadRequest(new { erro = "A senha deve ter pelo menos 6 caracteres." });

        var tenant = await _platform.Tenants
            .FirstOrDefaultAsync(t => t.TokenResetSenha == req.Token && t.Ativo);

        if (tenant == null)
            return BadRequest(new { erro = "Link invalido ou expirado." });

        if (tenant.TokenResetExpira == null || tenant.TokenResetExpira < DateTime.UtcNow)
            return BadRequest(new { erro = "Link expirado. Solicite um novo." });

        if (string.IsNullOrWhiteSpace(tenant.SchemaName))
            return BadRequest(new { erro = "Clinica nao configurada corretamente." });

        // Atualiza senha do owner no schema do tenant
        var connStr = _cfg.GetConnectionString("Default")!;
        var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connStr).Options;
        using var db = new TenantDbContext(opts, tenant.SchemaName);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == tenant.Email && u.Ativo);

        if (user == null)
            return BadRequest(new { erro = "Usuario nao encontrado." });

        user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha, workFactor: 11);
        await db.SaveChangesAsync();

        // Invalida o token
        tenant.TokenResetSenha  = null;
        tenant.TokenResetExpira = null;
        await _platform.SaveChangesAsync();

        return Ok(new { mensagem = "Senha redefinida com sucesso! Faca o login." });
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task EnviarEmailReset(string email, string nomeClinica, string linkReset)
    {
        var smtpHost  = _cfg["Smtp:Host"]      ?? Environment.GetEnvironmentVariable("Smtp__Host");
        var smtpPorta = int.Parse(_cfg["Smtp:Porta"] ?? Environment.GetEnvironmentVariable("Smtp__Porta") ?? "587");
        var smtpUser  = _cfg["Smtp:Usuario"]   ?? Environment.GetEnvironmentVariable("Smtp__Usuario");
        var smtpSenha = _cfg["Smtp:Senha"]     ?? Environment.GetEnvironmentVariable("Smtp__Senha");
        var smtpSsl   = bool.Parse(_cfg["Smtp:Ssl"] ?? Environment.GetEnvironmentVariable("Smtp__Ssl") ?? "true");
        var remetente = _cfg["Smtp:Remetente"] ?? smtpUser;

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
        {
            Console.WriteLine("[Reset] SMTP nao configurado — email nao enviado.");
            return;
        }

        using var client = new SmtpClient(smtpHost, smtpPorta)
        {
            EnableSsl   = smtpSsl,
            Credentials = new NetworkCredential(smtpUser, smtpSenha)
        };

        var body = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;'>
  <div style='background: linear-gradient(135deg, #0f172a 0%, #1e3a5f 100%); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
    <h1 style='color: white; margin: 0; font-size: 24px;'>VetClinica</h1>
    <p style='color: #94a3b8; margin: 8px 0 0 0;'>Redefinicao de Senha</p>
  </div>
  <div style='background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0;'>
    <h2 style='color: #0f172a; margin-top: 0;'>Solicitacao de nova senha</h2>
    <p>Recebemos uma solicitacao de redefinicao de senha para a clinica <strong>{nomeClinica}</strong>.</p>
    <p style='color: #64748b; font-size: 13px;'>O link abaixo expira em <strong>30 minutos</strong>.</p>
    <div style='text-align: center; margin: 25px 0;'>
      <a href='{linkReset}' style='background: #3b82f6; color: white; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: bold; font-size: 16px; display: inline-block;'>
        Redefinir Senha
      </a>
    </div>
    <p style='font-size: 12px; color: #94a3b8;'>Se nao foi voce, ignore este e-mail. Sua senha permanece a mesma.</p>
    <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;'>
    <p style='font-size: 12px; color: #94a3b8;'>Ou copie e cole este link no navegador:<br>
      <a href='{linkReset}' style='color: #3b82f6; word-break: break-all;'>{linkReset}</a>
    </p>
  </div>
  <div style='background: #f1f5f9; padding: 15px; border-radius: 0 0 12px 12px; text-align: center;'>
    <p style='color: #94a3b8; font-size: 12px; margin: 0;'>VetClinica by <strong>Ketra Solucoes Inteligentes</strong></p>
  </div>
</body>
</html>";

        var msg = new MailMessage
        {
            From       = new MailAddress(remetente!, "VetClinica by Ketra"),
            Subject    = $"Redefinicao de senha - {nomeClinica}",
            IsBodyHtml = true,
            Body       = body
        };
        msg.To.Add(email);
        await client.SendMailAsync(msg);
        Console.WriteLine($"[Reset] Email enviado para {email}");
    }

    private async Task EnviarWhatsAppReset(VetClinica.API.Models.Tenant tenant, string linkReset)
    {
        var connStr = _cfg.GetConnectionString("Default")!;
        var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connStr).Options;
        using var db = new TenantDbContext(opts, tenant.SchemaName!);

        var botConfig = await db.BotConfigs.FirstOrDefaultAsync();
        if (botConfig == null || string.IsNullOrWhiteSpace(botConfig.MetaToken)) return;
        if (string.IsNullOrWhiteSpace(botConfig.MetaPhoneNumberId)) return;

        // Formata numero BR: remove mascara, garante 55
        var fone = (tenant.Telefone ?? "")
            .Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "").Replace("+", "");
        if (!fone.StartsWith("55")) fone = "55" + fone;

        var msgText = $"VetClinica - Redefinicao de senha\n\nClique no link abaixo para criar uma nova senha (valido por 30 min):\n{linkReset}";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", botConfig.MetaToken);

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            messaging_product = "whatsapp",
            to   = fone,
            type = "text",
            text = new { body = msgText }
        });

        var response = await http.PostAsync(
            $"https://graph.facebook.com/v18.0/{botConfig.MetaPhoneNumberId}/messages",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        Console.WriteLine($"[Reset] WhatsApp status: {response.StatusCode}");
    }
}
