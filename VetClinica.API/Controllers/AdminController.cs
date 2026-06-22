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
    private readonly IEmailService          _email;
    private readonly TenantDbContextFactory _factory;
    private readonly VetClinica.API.Services.Payments.AsaasAssinaturaService _asaasAssinatura;

    public AdminController(PlatformDbContext platform, TenantContext t, ProvisionamentoService prov, IConfiguration cfg, IEmailService email, TenantDbContextFactory factory, VetClinica.API.Services.Payments.AsaasAssinaturaService asaasAssinatura)
    { _platform = platform; _t = t; _prov = prov; _cfg = cfg; _email = email; _factory = factory; _asaasAssinatura = asaasAssinatura; }

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
                t.Email, t.SchemaName, t.CriadoEm, t.TrialExpiraEm,
                t.StatusPagamento, t.ProximoFaturamento
            })
            .ToListAsync();
        return Ok(clinicas);
    }

    [HttpPost("clinicas/{id}/resetar-senha")]
    public async Task<IActionResult> ResetarSenha(Guid id)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _platform.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        if (string.IsNullOrWhiteSpace(t.SchemaName))
            return BadRequest(new { erro = "Clinica sem schema configurado." });

        using var db = _factory.CreateForSchema(t.SchemaName!);
        var owner = await db.Users.FirstOrDefaultAsync(u => u.Email == t.Email && u.Ativo && u.Papel == "owner");
        if (owner == null)
            return NotFound(new { erro = "Usuario principal (owner) nao encontrado nessa clinica." });

        var novaSenha = ProvisionamentoService.GerarSenhaTemporaria();
        owner.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha, workFactor: 11);
        await db.SaveChangesAsync();

        var emailEnviado = false;
        if (!string.IsNullOrWhiteSpace(owner.Email))
        {
            var html = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
  <div style='background: #0f172a; padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
    <h1 style='color: white; margin: 0;'>VetClinica</h1>
  </div>
  <div style='background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0;'>
    <h2>Sua senha foi redefinida</h2>
    <p>Clinica: <strong>{t.Nome}</strong></p>
    <p>O administrador da plataforma gerou uma nova senha temporaria para o seu login:</p>
    <div style='text-align: center; margin: 25px 0;'>
      <span style='background: #0f172a; color: white; padding: 14px 32px; border-radius: 8px; font-weight: bold; font-family: monospace; font-size: 18px;'>{novaSenha}</span>
    </div>
    <p>Login: <strong>{owner.Email}</strong></p>
    <p style='font-size: 12px; color: #94a3b8;'>Recomendamos trocar essa senha assim que possivel apos o login.</p>
  </div>
</body></html>";
            var r = await _email.EnviarAsync(owner.Email, "Sua senha foi redefinida - VetClinica", html);
            emailEnviado = r.Ok;
        }

        return Ok(new { loginEmail = owner.Email, senhaTemporaria = novaSenha, emailEnviado });
    }

    [HttpPut("clinicas/{id}/pagamento")]
    public async Task<IActionResult> AtualizarPagamento(Guid id, AtualizarPagamentoRequest req)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _platform.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.StatusPagamento    = string.IsNullOrWhiteSpace(req.StatusPagamento) ? null : req.StatusPagamento;
        t.ProximoFaturamento = req.ProximoFaturamento;
        await _platform.SaveChangesAsync();
        return NoContent();
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
        var resendKey = await Cfg("resend_api_key");
        var graphSecret = await Cfg("graph_client_secret");
        return Ok(new {
            provider          = await Cfg("email_provider")    ?? "resend",
            resendApiKey      = "",   // nunca retorna a chave
            resendConfigurado = !string.IsNullOrWhiteSpace(resendKey),
            resendRemetente   = await Cfg("resend_remetente") ?? "",
            graphTenantId     = await Cfg("graph_tenant_id")  ?? "",
            graphClientId     = await Cfg("graph_client_id")  ?? "",
            graphClientSecret = "",   // nunca retorna o secret
            graphConfigurado  = !string.IsNullOrWhiteSpace(graphSecret),
            graphRemetente    = await Cfg("graph_remetente")  ?? "",
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

        if (!string.IsNullOrWhiteSpace(req.Provider))
            await SetCfg("email_provider", req.Provider.Trim().ToLowerInvariant());

        // Resend
        if (!string.IsNullOrWhiteSpace(req.ResendApiKey))
            await SetCfg("resend_api_key", req.ResendApiKey.Trim());
        if (req.ResendRemetente != null)
            await SetCfg("resend_remetente", req.ResendRemetente.Trim());

        // Microsoft Graph (Office 365)
        if (!string.IsNullOrWhiteSpace(req.GraphTenantId))
            await SetCfg("graph_tenant_id", req.GraphTenantId.Trim());
        if (!string.IsNullOrWhiteSpace(req.GraphClientId))
            await SetCfg("graph_client_id", req.GraphClientId.Trim());
        if (!string.IsNullOrWhiteSpace(req.GraphClientSecret))
            await SetCfg("graph_client_secret", req.GraphClientSecret.Trim());
        if (req.GraphRemetente != null)
            await SetCfg("graph_remetente", req.GraphRemetente.Trim());

        // SMTP (legado / plano Pro)
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

        var para = !string.IsNullOrWhiteSpace(destino)
            ? destino
            : (await Cfg("graph_remetente") ?? await Cfg("resend_remetente") ?? await Cfg("smtp_usuario"));

        if (string.IsNullOrWhiteSpace(para))
            return BadRequest(new { erro = "Informe um email de destino para o teste." });

        var html = "<p>Email de teste enviado com sucesso pelo painel admin do VetClinica.</p>";
        var r = await _email.EnviarAsync(para!, "Teste de envio - VetClinica", html);

        if (r.Ok)
            return Ok(new { mensagem = $"Email de teste enviado para {para} via {r.Provider}." });

        return BadRequest(new { erro = r.Erro, provider = r.Provider });
    }

    // ── Assinatura SaaS (Asaas — conta da Ketra, nao a de cada clinica) ──

    [HttpGet("asaas-saas-config")]
    public async Task<IActionResult> GetAsaasSaasConfig()
    {
        var g = Guard(); if (g != null) return g;
        var apiKey = await Cfg("asaas_saas_api_key");
        var webhookToken = await Cfg("asaas_saas_webhook_token");
        return Ok(new {
            apiKey              = "",   // nunca retorna a chave
            apiKeyConfigurada   = !string.IsNullOrWhiteSpace(apiKey),
            ambiente            = await Cfg("asaas_saas_ambiente") ?? "sandbox",
            webhookToken        = "",   // nunca retorna o token
            webhookConfigurado  = !string.IsNullOrWhiteSpace(webhookToken)
        });
    }

    [HttpPost("asaas-saas-config")]
    public async Task<IActionResult> SaveAsaasSaasConfig(AsaasSaasConfigRequest req)
    {
        var g = Guard(); if (g != null) return g;
        if (!string.IsNullOrWhiteSpace(req.ApiKey))
            await SetCfg("asaas_saas_api_key", req.ApiKey.Trim());
        if (!string.IsNullOrWhiteSpace(req.Ambiente))
            await SetCfg("asaas_saas_ambiente", req.Ambiente.Trim().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(req.WebhookToken))
            await SetCfg("asaas_saas_webhook_token", req.WebhookToken.Trim());
        await _platform.SaveChangesAsync();
        return Ok(new { mensagem = "Configuracao da assinatura SaaS salva." });
    }

    // Precos fixos no servidor (nao confia em valor vindo do cliente).
    // Mantenha igual ao frontend/src/pages/Planos.jsx.
    private static readonly Dictionary<string, decimal> PrecosPlanos = new() {
        ["starter"] = 149m,
        ["profissional"] = 289m
    };

    [HttpPost("clinicas/{id}/gerar-assinatura")]
    public async Task<IActionResult> GerarAssinatura(Guid id, GerarAssinaturaRequest req)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _platform.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        if (!PrecosPlanos.TryGetValue(req.PlanoId, out var valor))
            return BadRequest(new { erro = "Plano invalido. Use 'starter' ou 'profissional' (Enterprise e negociado direto)." });

        var resultado = await _asaasAssinatura.CriarAssinatura(t, valor, req.PlanoId, $"Assinatura VetClinica — plano {req.PlanoId}");
        if (!resultado.Ok)
            return BadRequest(new { erro = resultado.Erro });

        if (!string.IsNullOrWhiteSpace(resultado.ClienteId))    t.AsaasClienteId    = resultado.ClienteId;
        if (!string.IsNullOrWhiteSpace(resultado.AssinaturaId)) t.AsaasAssinaturaId = resultado.AssinaturaId;
        await _platform.SaveChangesAsync();

        return Ok(new { assinaturaId = resultado.AssinaturaId, invoiceUrl = resultado.InvoiceUrl });
    }
}
