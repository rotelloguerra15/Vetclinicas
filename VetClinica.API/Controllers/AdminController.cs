using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly PlatformDbContext      _platform;
    private readonly TenantContext          _t;
    private readonly ProvisionamentoService _prov;
    private readonly IConfiguration         _cfg;

    public AdminController(PlatformDbContext platform, TenantContext t, ProvisionamentoService prov, IConfiguration cfg)
    { _platform = platform; _t = t; _prov = prov; _cfg = cfg; }

    private IActionResult? Guard() =>
        _t.IsPlatformAdmin ? null : StatusCode(403, new { erro = "Acesso restrito ao administrador da plataforma" });

    [HttpGet("clinicas")]
    public async Task<IActionResult> Clinicas()
    {
        var g = Guard(); if (g != null) return g;
        var clinicas = await _platform.Tenants
            .OrderByDescending(t => t.CriadoEm)
            .Select(t => new {
                t.Id, t.Nome, t.Plano, t.Ativo,
                Suspenso       = t.SuspensoEm != null,
                t.Email,
                t.SchemaName,
                t.CriadoEm,
                t.TrialExpiraEm
            })
            .ToListAsync();
        return Ok(clinicas);
    }

    [HttpPost("clinicas")]
    public async Task<IActionResult> CriarClinica(NovaClinicaRequest req)
    {
        var g = Guard(); if (g != null) return g;
        var jaExiste = await _platform.Tenants.AnyAsync(t => t.Email == req.EmailDono);
        if (jaExiste) return BadRequest(new { erro = "Ja existe uma clinica com esse email" });

        var r = await _prov.CriarClinica(req.NomeClinica, req.Plano, req.NomeDono,
            req.EmailDono, req.Telefone, req.Tagline);

        return Ok(new { r.TenantId, r.LoginEmail, r.SenhaTemporaria, r.SchemaName });
    }

    [HttpPut("clinicas/{id}/suspender")]
    public async Task<IActionResult> Suspender(Guid id)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _platform.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.SuspensoEm = DateTime.UtcNow;
        await _platform.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("clinicas/{id}/reativar")]
    public async Task<IActionResult> Reativar(Guid id)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _platform.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.SuspensoEm = null;
        await _platform.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("clinicas/{id}/plano")]
    public async Task<IActionResult> MudarPlano(Guid id, MudarPlanoRequest req)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _platform.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.Plano = req.Plano;
        await _platform.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("metricas")]
    public async Task<IActionResult> Metricas()
    {
        var g = Guard(); if (g != null) return g;
        return Ok(new {
            totalClinicas     = await _platform.Tenants.CountAsync(),
            clinicasAtivas    = await _platform.Tenants.CountAsync(t => t.Ativo && t.SuspensoEm == null),
            clinicasSuspensas = await _platform.Tenants.CountAsync(t => t.SuspensoEm != null),
            totalPets         = 0,
            porPlano          = await _platform.Tenants
                .GroupBy(t => t.Plano)
                .Select(grp => new { plano = grp.Key, qtd = grp.Count() })
                .ToListAsync()
        });
    }

    [HttpGet("preview-schema")]
    public IActionResult PreviewSchema([FromQuery] string nome)
    {
        var g = Guard(); if (g != null) return g;
        return Ok(new { schema = ProvisionamentoService.GerarSchemaName(nome) });
    }

    // ── SMTP Config ───────────────────────────────────────────────────

    [HttpGet("smtp-config")]
    public IActionResult GetSmtpConfig()
    {
        var g = Guard(); if (g != null) return g;
        return Ok(new {
            host      = _cfg["Smtp:Host"]      ?? Environment.GetEnvironmentVariable("Smtp__Host")      ?? "",
            porta     = _cfg["Smtp:Porta"]     ?? Environment.GetEnvironmentVariable("Smtp__Porta")     ?? "587",
            usuario   = _cfg["Smtp:Usuario"]   ?? Environment.GetEnvironmentVariable("Smtp__Usuario")   ?? "",
            senha     = "",  // nunca retorna a senha
            ssl       = _cfg["Smtp:Ssl"]       ?? Environment.GetEnvironmentVariable("Smtp__Ssl")       ?? "true",
            remetente = _cfg["Smtp:Remetente"] ?? Environment.GetEnvironmentVariable("Smtp__Remetente") ?? ""
        });
    }

    [HttpPost("smtp-config")]
    public IActionResult SaveSmtpConfig([FromBody] SmtpConfigRequest req)
    {
        var g = Guard(); if (g != null) return g;
        // As variaveis sao gerenciadas no Railway — aqui apenas validamos e confirmamos
        // O usuario deve atualizar as variaveis no painel do Railway manualmente
        // Este endpoint serve para documentar o que deve ser configurado
        return Ok(new {
            mensagem = "Configuracoes recebidas. Atualize as variaveis no Railway com os valores abaixo.",
            variaveis = new {
                Smtp__Host      = req.Host,
                Smtp__Porta     = req.Porta,
                Smtp__Usuario   = req.Usuario,
                Smtp__Senha     = req.Senha,
                Smtp__Ssl       = req.Ssl,
                Smtp__Remetente = req.Remetente
            }
        });
    }

    [HttpPost("smtp-teste")]
    public async Task<IActionResult> TestarSmtp([FromQuery] string? destino)
    {
        var g = Guard(); if (g != null) return g;

        var smtpHost  = _cfg["Smtp:Host"]      ?? Environment.GetEnvironmentVariable("Smtp__Host");
        var smtpPorta = int.Parse(_cfg["Smtp:Porta"] ?? Environment.GetEnvironmentVariable("Smtp__Porta") ?? "587");
        var smtpUser  = _cfg["Smtp:Usuario"]   ?? Environment.GetEnvironmentVariable("Smtp__Usuario");
        var smtpSenha = _cfg["Smtp:Senha"]     ?? Environment.GetEnvironmentVariable("Smtp__Senha");
        var smtpSsl   = bool.Parse(_cfg["Smtp:Ssl"] ?? Environment.GetEnvironmentVariable("Smtp__Ssl") ?? "true");
        var remetente = _cfg["Smtp:Remetente"] ?? smtpUser;

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
            return BadRequest(new { erro = "SMTP nao configurado nas variaveis de ambiente do Railway." });

        var para = !string.IsNullOrWhiteSpace(destino) ? destino : smtpUser;
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var client = new SmtpClient(smtpHost, smtpPorta)
            {
                EnableSsl        = smtpSsl,
                Credentials      = new NetworkCredential(smtpUser, smtpSenha),
                Timeout          = 15000,
                DeliveryMethod   = SmtpDeliveryMethod.Network
            };
            var msg = new MailMessage
            {
                From       = new MailAddress(remetente!, "VetClinica by Ketra"),
                Subject    = "Teste de SMTP - VetClinica",
                IsBodyHtml = true,
                Body       = "<p>Email de teste enviado com sucesso pelo painel admin do VetClinica.</p>"
            };
            msg.To.Add(para);
            await client.SendMailAsync(msg, cts.Token);
            return Ok(new { mensagem = $"Email de teste enviado para {para}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace?.Split("
").FirstOrDefault() });
        }
    }
}
