using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using VetClinica.API.Data;
using VetClinica.API.Services;
using VetClinica.API.Services.Certificado;

namespace VetClinica.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/trial")]
public class TrialController : ControllerBase
{
    private readonly PlatformDbContext      _platform;
    private readonly ProvisionamentoService _prov;
    private readonly IConfiguration         _cfg;

    public TrialController(
        PlatformDbContext platform,
        ProvisionamentoService prov,
        IConfiguration cfg)
    {
        _platform = platform;
        _prov     = prov;
        _cfg      = cfg;
    }

    public record TrialRequest(
        // Dados da clínica
        string NomeClinica,
        string NomeDono,
        string Email,
        string Telefone,
        // Dados fiscais (para futura NF)
        string? RazaoSocial,
        string? Cnpj,
        string? InscricaoEstadual,
        string? Logradouro,
        string? Numero,
        string? Complemento,
        string? Bairro,
        string? Cidade,
        string? Estado,
        string? Cep
    );

    [HttpPost]
    public async Task<IActionResult> CadastrarTrial(TrialRequest req)
    {
        // Validações básicas
        if (string.IsNullOrWhiteSpace(req.NomeClinica))
            return BadRequest(new { erro = "Nome da clínica é obrigatório." });
        if (string.IsNullOrWhiteSpace(req.NomeDono))
            return BadRequest(new { erro = "Seu nome é obrigatório." });
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return BadRequest(new { erro = "E-mail inválido." });

        // Verifica duplicidade de email
        var jaExiste = await _platform.Tenants
            .AnyAsync(t => t.Email == req.Email);
        if (jaExiste)
            return Ok(new { jaExiste = true, mensagem = "Ja existe uma conta com este e-mail.", urlLogin = $"{_cfg["App:FrontendUrl"] ?? "https://vetclinicas.vercel.app"}/login" });

        // Cria o tenant + dispara provisionamento em background
        var resultado = await _prov.CriarClinica(
            nomeClinica: req.NomeClinica.Trim(),
            plano:       "trial",
            nomeDono:    req.NomeDono.Trim(),
            emailDono:   req.Email.Trim().ToLower(),
            telefone:    req.Telefone?.Trim(),
            tagline:     "Gestao Inteligente para Clinicas Veterinarias"
        );

        // Envia email em background (nao bloqueia)
        var reqCopy = req;
        var senhaTemp = resultado.SenhaTemporaria;
        _ = Task.Run(async () => {
            try { await EnviarEmailBoasVindas(reqCopy, resultado.LoginEmail, senhaTemp); }
            catch { }
        });

        // Retorna imediatamente com as credenciais
        return Ok(new
        {
            mensagem   = "Conta criada com sucesso!",
            loginEmail = resultado.LoginEmail,
            senha      = resultado.SenhaTemporaria,
            emailEnviado = true,
            urlLogin   = $"{_cfg["App:FrontendUrl"] ?? "https://vetclinicas.vercel.app"}/login"
        });
    }

    [HttpGet("verificar-email")]
    public async Task<IActionResult> VerificarEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest();
        var existe = await _platform.Tenants.AnyAsync(t => t.Email == email.ToLower());
        return Ok(new { disponivel = !existe });
    }

    private async Task SalvarDadosFiscais(string schemaName, TrialRequest req, Guid tenantId)
    {
        try
        {
            var connStr = _cfg.GetConnectionString("Default")!;
            var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TenantDbContext>()
                .UseNpgsql(connStr).Options;
            using var db = new TenantDbContext(opts, schemaName);

            var p = await db.ParametrosSistema
                .FirstOrDefaultAsync(x => x.TenantId == tenantId);

            if (p != null)
            {
                // Armazena dados fiscais nos campos disponíveis
                // RazaoSocial -> contabil_nome (campo mais próximo)
                p.ContabilNome = req.RazaoSocial;
                await db.SaveChangesAsync();
            }
        }
        catch { /* não bloqueia o cadastro se falhar */ }
    }

    private async Task<bool> EnviarEmailBoasVindas(TrialRequest req, string email, string senha)
    {
        try
        {
            // Usa SMTP da Ketra configurado nas variáveis de ambiente
            var smtpHost  = _cfg["Smtp:Host"]     ?? Environment.GetEnvironmentVariable("Smtp__Host");
            var smtpPorta = int.Parse(_cfg["Smtp:Porta"] ?? Environment.GetEnvironmentVariable("Smtp__Porta") ?? "587");
            var smtpUser  = _cfg["Smtp:Usuario"]  ?? Environment.GetEnvironmentVariable("Smtp__Usuario");
            var smtpSenha = _cfg["Smtp:Senha"]    ?? Environment.GetEnvironmentVariable("Smtp__Senha");
            var smtpSsl   = bool.Parse(_cfg["Smtp:Ssl"] ?? Environment.GetEnvironmentVariable("Smtp__Ssl") ?? "true");
            var remetente = _cfg["Smtp:Remetente"] ?? Environment.GetEnvironmentVariable("Smtp__Remetente") ?? smtpUser;

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
                return false;

            using var client = new SmtpClient(smtpHost, smtpPorta)
            {
                EnableSsl   = smtpSsl,
                Credentials = new NetworkCredential(smtpUser, smtpSenha)
            };

            var frontendUrl = _cfg["App:FrontendUrl"] ?? "https://vetclinicas.vercel.app";

            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;'>

  <div style='background: linear-gradient(135deg, #0f172a 0%, #1e3a5f 100%); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
    <h1 style='color: white; margin: 0; font-size: 24px;'>🐾 VetClinica</h1>
    <p style='color: #94a3b8; margin: 8px 0 0 0;'>Gestão Inteligente para Clínicas Veterinárias</p>
  </div>

  <div style='background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0;'>
    <h2 style='color: #0f172a; margin-top: 0;'>Olá, {req.NomeDono}! 👋</h2>
    <p>Sua conta no <strong>VetClinica</strong> foi criada com sucesso! Você tem <strong>14 dias gratuitos</strong> para explorar todos os recursos.</p>

    <div style='background: white; border: 2px solid #3b82f6; border-radius: 8px; padding: 20px; margin: 20px 0;'>
      <h3 style='color: #1e40af; margin-top: 0;'>🔑 Seus dados de acesso</h3>
      <p style='margin: 8px 0;'><strong>Clínica:</strong> {req.NomeClinica}</p>
      <p style='margin: 8px 0;'><strong>E-mail:</strong> {email}</p>
      <p style='margin: 8px 0;'><strong>Senha temporária:</strong> <span style='font-family: monospace; background: #f1f5f9; padding: 2px 8px; border-radius: 4px; font-size: 16px; letter-spacing: 2px;'>{senha}</span></p>
      <p style='margin: 8px 0; font-size: 12px; color: #64748b;'>Recomendamos alterar a senha após o primeiro acesso.</p>
    </div>

    <div style='text-align: center; margin: 25px 0;'>
      <a href='{frontendUrl}/login' style='background: #3b82f6; color: white; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: bold; font-size: 16px; display: inline-block;'>
        Acessar o Sistema →
      </a>
    </div>

    <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 25px 0;'>

    <h3 style='color: #0f172a;'>✅ O que você pode fazer agora:</h3>
    <ul style='color: #475569; line-height: 1.8;'>
      <li>Cadastrar tutores e pets</li>
      <li>Gerenciar agendamentos</li>
      <li>Emitir receituários veterinários</li>
      <li>Controlar financeiro e caixa</li>
      <li>Configurar WhatsApp Bot</li>
      <li>Diagnóstico assistido por IA</li>
    </ul>

    <div style='background: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; border-radius: 0 8px 8px 0; margin-top: 20px;'>
      <p style='margin: 0; color: #1e40af;'><strong>🎓 Treinamento gratuito</strong><br>
      Agende uma sessão de treinamento com nossa equipe.<br>
      Entre em contato: <a href='mailto:suporte@ketra.com.br' style='color: #3b82f6;'>suporte@ketra.com.br</a></p>
    </div>
  </div>

  <div style='background: #f1f5f9; padding: 20px; border-radius: 0 0 12px 12px; text-align: center;'>
    <p style='color: #94a3b8; font-size: 12px; margin: 0;'>
      VetClinica by <strong>Ketra Soluções Inteligentes</strong><br>
      Dúvidas? <a href='mailto:suporte@ketra.com.br' style='color: #64748b;'>suporte@ketra.com.br</a>
    </p>
  </div>

</body>
</html>";

            var msg = new MailMessage
            {
                From       = new MailAddress(remetente!, "VetClinica by Ketra"),
                Subject    = $"🐾 Bem-vindo ao VetClinica, {req.NomeDono}! Seus dados de acesso",
                IsBodyHtml = true,
                Body       = body
            };
            msg.To.Add(email);

            await client.SendMailAsync(msg);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Trial] Erro ao enviar email: {ex.Message}");
            return false;
        }
    }
}
