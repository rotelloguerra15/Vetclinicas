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

    // ── Helper: le config do banco ────────────────────────────────────
    private async Task<string?> Cfg(string chave) =>
        (await _platform.Configuracoes.FirstOrDefaultAsync(c => c.Chave == chave))?.Valor;

    private async Task SetCfg(string chave, string valor)
    {
        var c = await _platform.Configuracoes.FirstOrDefaultAsync(x => x.Chave == chave);
        if (c == null)
            _platform.Configuracoes.Add(new VetClinica.API.Models.Configuracao { Chave = chave, Valor = valor, AtualizadoEm = DateTime.UtcNow });
        else
        { c.Valor = valor; c.AtualizadoEm = DateTime.UtcNow; }
    }

    // ── Clinicas ──────────────────────────────────────────────────────

    [HttpGet("clinicas")]
    public async Task<IActionResult> Clinicas()
    {
        var g = Guard(); if (g != null) return g;
        var clinicas = await _platform.Tenants
            .OrderByDescending(t => t.CriadoEm)
            .Select(t => new {
                t.Id, t.Nome, t.Plano, t.Ativo,
                Suspenso    = t.SuspensoEm != null,
                t.Email, t.SchemaName, t.CriadoEm, t.TrialExpiraEm
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
        var r = await _prov.CriarClinica(req.NomeClinica, req.Plano, req.NomeDono, req.EmailDono, req.Telefone, req.Tagline);
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

    // ── SMTP Config — salva/le do banco ───────────────────────────────

    [HttpGet("smtp-config")]
    public async Task<IActionResult> GetSmtpConfig()
    {
        var g = Guard(); if (g != null) return g;
        return Ok(new {
            host      = await Cfg("smtp_host")      ?? "",
            porta     = await Cfg("smtp_porta")     ?? "587",
            usuario   = await Cfg("smtp_usuario")   ?? "",
            senha     = "",   // nunca retorna a senha
            ssl       = await Cfg("smtp_ssl")       ?? "false",
            remetente = await Cfg("smtp_remetente") ?? ""
        });
    }

    [HttpPost("smtp-config")]
    public async Task<IActionResult> SaveSmtpConfig([FromBody] SmtpConfigRequest req)
    {
        var g = Guard(); if (g != null) return g;
        await SetCfg("smtp_host",      req.Host);
        await SetCfg("smtp_porta",     req.Porta);
        await SetCfg("smtp_usuario",   req.Usuario);
        await SetCfg("smtp_ssl",       req.Ssl);
        await SetCfg("smtp_remetente", req.Remetente);
        if (!string.IsNullOrWhiteSpace(req.Senha))
            await SetCfg("smtp_senha", req.Senha);
        await _platform.SaveChangesAsync();
        return Ok(new { mensagem = "Configuracoes salvas com sucesso." });
    }

    [HttpPost("smtp-teste")]
    public async Task<IActionResult> TestarSmtp([FromQuery] string? destino)
    {
        var g = Guard(); if (g != null) return g;

        var smtpHost  = await Cfg("smtp_host")      ?? _cfg["Smtp:Host"]    ?? Environment.GetEnvironmentVariable("Smtp__Host");
        var smtpPorta = int.Parse(await Cfg("smtp_porta")     ?? _cfg["Smtp:Porta"]   ?? Environment.GetEnvironmentVariable("Smtp__Porta")   ?? "587");
        var smtpUser  = await Cfg("smtp_usuario")   ?? _cfg["Smtp:Usuario"] ?? Environment.GetEnvironmentVariable("Smtp__Usuario");
        var smtpSenha = await Cfg("smtp_senha")     ?? _cfg["Smtp:Senha"]   ?? Environment.GetEnvironmentVariable("Smtp__Senha");
        var smtpSsl   = bool.Parse(await Cfg("smtp_ssl") ?? _cfg["Smtp:Ssl"] ?? Environment.GetEnvironmentVariable("Smtp__Ssl") ?? "false");
        var remetente = await Cfg("smtp_remetente") ?? smtpUser;

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
            return BadRequest(new { erro = "SMTP nao configurado. Preencha as configuracoes acima." });

        var para = !string.IsNullOrWhiteSpace(destino) ? destino : smtpUser;

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var client = new SmtpClient(smtpHost, smtpPorta)
            {
                EnableSsl      = smtpSsl,
                Credentials    = new NetworkCredential(smtpUser, smtpSenha),
                Timeout        = 15000,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            var msg = new MailMessage
            {
                From       = new MailAddress(remetente!, "VetClinica by Ketra"),
                Subject    = "Teste de SMTP - VetClinica",
                IsBodyHtml = true,
                Body       = "<p>Email de teste enviado com sucesso pelo painel admin do VetClinica.</p>"
            };
            msg.To.Add(para!);
            await client.SendMailAsync(msg, cts.Token);
            return Ok(new { mensagem = $"Email de teste enviado para {para}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}
