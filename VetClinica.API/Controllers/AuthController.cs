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
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService auth, PlatformDbContext platform, IConfiguration cfg, ILogger<AuthController> logger)
    {
        _auth     = auth;
        _platform = platform;
        _cfg      = cfg;
        _logger   = logger;
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

    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { erro = "Informe o e-mail." });

        var tenant = await _platform.Tenants
            .FirstOrDefaultAsync(t => t.Email == req.Email.Trim().ToLower() && t.Ativo);

        if (tenant == null)
            return Ok(new { mensagem = "Se o e-mail existir, voce recebera as instrucoes." });

        var token  = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(20)).ToLower();
        var expira = DateTime.UtcNow.AddMinutes(30);

        tenant.TokenResetSenha  = token;
        tenant.TokenResetExpira = expira;
        await _platform.SaveChangesAsync();

        var frontendUrl = _cfg["App:FrontendUrl"] ?? "https://vetclinicas.vercel.app";
        var linkReset   = $"{frontendUrl}/redefinir-senha?token={token}";

        // Envia AGUARDANDO resultado para logar erro
        var emailOk = false;
        try
        {
            emailOk = await EnviarEmailReset(req.Email.Trim().ToLower(), tenant.Nome, linkReset);
        }
        catch (Exception ex)
        {
            _logger.LogError("[Reset] Erro email: {msg} | Inner: {inner}", ex.Message, ex.InnerException?.Message);
        }

        _logger.LogInformation("[Reset] Email enviado: {ok} para {email}", emailOk, req.Email);

        return Ok(new { mensagem = "Se o e-mail existir, voce recebera as instrucoes." });
    }

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

        tenant.TokenResetSenha  = null;
        tenant.TokenResetExpira = null;
        await _platform.SaveChangesAsync();

        return Ok(new { mensagem = "Senha redefinida com sucesso! Faca o login." });
    }

    private async Task<bool> EnviarEmailReset(string email, string nomeClinica, string linkReset)
    {
        var smtpHost  = _cfg["Smtp:Host"]      ?? Environment.GetEnvironmentVariable("Smtp__Host");
        var smtpPorta = int.Parse(_cfg["Smtp:Porta"] ?? Environment.GetEnvironmentVariable("Smtp__Porta") ?? "587");
        var smtpUser  = _cfg["Smtp:Usuario"]   ?? Environment.GetEnvironmentVariable("Smtp__Usuario");
        var smtpSenha = _cfg["Smtp:Senha"]     ?? Environment.GetEnvironmentVariable("Smtp__Senha");
        var smtpSsl   = bool.Parse(_cfg["Smtp:Ssl"] ?? Environment.GetEnvironmentVariable("Smtp__Ssl") ?? "true");
        var remetente = _cfg["Smtp:Remetente"] ?? smtpUser;

        _logger.LogInformation("[Reset] SMTP config: host={host} porta={porta} user={user} ssl={ssl}", smtpHost, smtpPorta, smtpUser, smtpSsl);

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
        {
            _logger.LogError("[Reset] SMTP nao configurado.");
            return false;
        }

        using var client = new SmtpClient(smtpHost, smtpPorta)
        {
            EnableSsl   = smtpSsl,
            Credentials = new NetworkCredential(smtpUser, smtpSenha)
        };

        var body = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
  <div style='background: #0f172a; padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
    <h1 style='color: white; margin: 0;'>VetClinica</h1>
  </div>
  <div style='background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0;'>
    <h2>Redefinicao de senha</h2>
    <p>Clinica: <strong>{nomeClinica}</strong></p>
    <p>Clique no link abaixo para criar uma nova senha (valido por 30 minutos):</p>
    <div style='text-align: center; margin: 25px 0;'>
      <a href='{linkReset}' style='background: #3b82f6; color: white; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: bold;'>
        Redefinir Senha
      </a>
    </div>
    <p style='font-size: 12px; color: #94a3b8;'>Se nao foi voce, ignore este e-mail.</p>
    <p style='font-size: 12px; color: #94a3b8;'>{linkReset}</p>
  </div>
</body></html>";

        var msg = new MailMessage
        {
            From       = new MailAddress(remetente!, "VetClinica by Ketra"),
            Subject    = $"Redefinicao de senha - {nomeClinica}",
            IsBodyHtml = true,
            Body       = body
        };
        msg.To.Add(email);
        await client.SendMailAsync(msg);
        _logger.LogInformation("[Reset] Email enviado com sucesso para {email}", email);
        return true;
    }
}
