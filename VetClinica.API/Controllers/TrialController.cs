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
    private readonly IEmailService          _email;

    public TrialController(
        PlatformDbContext platform,
        ProvisionamentoService prov,
        IConfiguration cfg,
        IEmailService email)
    {
        _platform = platform;
        _prov     = prov;
        _cfg      = cfg;
        _email    = email;
    }

    public record TrialRequest(
        string NomeClinica,
        string NomeDono,
        string Email,
        string Telefone,
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
        if (string.IsNullOrWhiteSpace(req.NomeClinica))
            return BadRequest(new { erro = "Nome da clinica e obrigatorio." });
        if (string.IsNullOrWhiteSpace(req.NomeDono))
            return BadRequest(new { erro = "Seu nome e obrigatorio." });
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return BadRequest(new { erro = "E-mail invalido." });

        var emailNorm = req.Email.Trim().ToLower();

        var jaExiste = await _platform.Tenants
            .AnyAsync(t => t.Email == emailNorm);
        if (jaExiste)
            return Ok(new
            {
                jaExiste   = true,
                mensagem   = "Ja existe uma conta com este e-mail.",
                urlLogin   = $"{FrontendUrl}/login"
            });

        // Cria tenant + provisiona schema
        var resultado = await _prov.CriarClinica(
            nomeClinica: req.NomeClinica.Trim(),
            plano:       "trial",
            nomeDono:    req.NomeDono.Trim(),
            emailDono:   emailNorm,
            telefone:    req.Telefone?.Trim(),
            tagline:     "Gestao Inteligente para Clinicas Veterinarias"
        );

        // Grava trial_expira_em = criado_em + 14 dias
        var tenant = await _platform.Tenants.FirstOrDefaultAsync(t => t.Id == resultado.TenantId);
        DateTime trialExpiraEm = DateTime.UtcNow.AddDays(14);
        if (tenant != null)
        {
            tenant.TrialExpiraEm = trialExpiraEm;
            await _platform.SaveChangesAsync();
        }

        // Gera link de redefinicao de senha (para o email de boas-vindas)
        var tokenReset = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(20)).ToLower();
        if (tenant != null)
        {
            tenant.TokenResetSenha  = tokenReset;
            tenant.TokenResetExpira = DateTime.UtcNow.AddHours(72); // 3 dias para primeiro acesso
            await _platform.SaveChangesAsync();
        }
        var linkReset = $"{FrontendUrl}/redefinir-senha?token={tokenReset}";

        var reqCopy    = req;
        var senhaTemp  = resultado.SenhaTemporaria;
        var loginEmail = resultado.LoginEmail;

        // Envio sincrono: o Resend e via HTTP (rapido) e queremos saber se chegou.
        // Nao roda em Task.Run porque o EmailService usa o PlatformDbContext (scoped),
        // que seria descartado ao fim do request.
        var emailEnviado = false;
        try
        {
            emailEnviado = await EnviarEmailBoasVindas(reqCopy, loginEmail, senhaTemp, trialExpiraEm, linkReset);
        }
        catch (Exception ex) { Console.WriteLine($"[Trial] Email erro: {ex.Message}"); }

        return Ok(new
        {
            mensagem      = "Conta criada com sucesso!",
            loginEmail    = resultado.LoginEmail,
            senha         = resultado.SenhaTemporaria,
            emailEnviado,
            trialExpiraEm = trialExpiraEm.ToString("yyyy-MM-dd"),
            diasRestantes = 14,
            urlLogin      = $"{FrontendUrl}/login"
        });
    }

    [HttpGet("verificar-email")]
    public async Task<IActionResult> VerificarEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest();
        var existe = await _platform.Tenants.AnyAsync(t => t.Email == email.ToLower());
        return Ok(new { disponivel = !existe });
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private string FrontendUrl => _cfg["App:FrontendUrl"] ?? "https://vetclinicas.vercel.app";

    private async Task<bool> EnviarEmailBoasVindas(TrialRequest req, string email, string senha, DateTime trialExpiraEm, string linkReset)
    {
        try
        {
            var dataExpiraBR = trialExpiraEm.ToLocalTime().ToString("dd/MM/yyyy");

            var body = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;'>

  <div style='background: linear-gradient(135deg, #0f172a 0%, #1e3a5f 100%); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
    <h1 style='color: white; margin: 0; font-size: 24px;'>VetClinica</h1>
    <p style='color: #94a3b8; margin: 8px 0 0 0;'>Gestao Inteligente para Clinicas Veterinarias</p>
  </div>

  <div style='background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0;'>
    <h2 style='color: #0f172a; margin-top: 0;'>Ola, {req.NomeDono}!</h2>
    <p>Sua conta no <strong>VetClinica</strong> foi criada com sucesso!</p>

    <div style='background: #fff7ed; border: 2px solid #fb923c; border-radius: 8px; padding: 15px; margin: 15px 0; text-align: center;'>
      <p style='margin: 0; color: #c2410c; font-size: 15px;'>
        Seu trial gratuito encerra em <strong>{dataExpiraBR}</strong>
      </p>
      <p style='margin: 4px 0 0 0; font-size: 12px; color: #9a3412;'>14 dias para explorar todos os recursos</p>
    </div>

    <div style='background: white; border: 2px solid #3b82f6; border-radius: 8px; padding: 20px; margin: 20px 0;'>
      <h3 style='color: #1e40af; margin-top: 0;'>Seus dados de acesso</h3>
      <p style='margin: 8px 0;'><strong>Clinica:</strong> {req.NomeClinica}</p>
      <p style='margin: 8px 0;'><strong>E-mail:</strong> {email}</p>
      <p style='margin: 8px 0;'><strong>Senha temporaria:</strong>
        <span style='font-family: monospace; background: #f1f5f9; padding: 2px 8px; border-radius: 4px; font-size: 16px; letter-spacing: 2px;'>{senha}</span>
      </p>
    </div>

    <div style='text-align: center; margin: 20px 0;'>
      <a href='{FrontendUrl}/login' style='background: #3b82f6; color: white; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: bold; font-size: 16px; display: inline-block;'>
        Acessar o Sistema
      </a>
    </div>

    <div style='background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 8px; padding: 15px; margin: 15px 0;'>
      <p style='margin: 0; color: #166534; font-size: 14px;'>
        Prefere definir sua propria senha? Clique abaixo (link valido por 72h):
      </p>
      <div style='text-align: center; margin-top: 12px;'>
        <a href='{linkReset}' style='background: #16a34a; color: white; padding: 10px 24px; border-radius: 6px; text-decoration: none; font-weight: bold; font-size: 14px; display: inline-block;'>
          Criar minha senha
        </a>
      </div>
    </div>

    <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 25px 0;'>

    <h3 style='color: #0f172a;'>O que voce pode fazer:</h3>
    <ul style='color: #475569; line-height: 1.8;'>
      <li>Cadastrar tutores e pets</li>
      <li>Gerenciar agendamentos</li>
      <li>Emitir receituarios veterinarios</li>
      <li>Controlar financeiro e caixa</li>
      <li>Configurar WhatsApp Bot</li>
      <li>Diagnostico assistido por IA</li>
    </ul>

    <div style='background: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; border-radius: 0 8px 8px 0; margin-top: 20px;'>
      <p style='margin: 0; color: #1e40af;'><strong>Treinamento gratuito</strong><br>
      Agende com nossa equipe:<br>
      <a href='mailto:suporte@ketra.com.br' style='color: #3b82f6;'>suporte@ketra.com.br</a></p>
    </div>
  </div>

  <div style='background: #f1f5f9; padding: 20px; border-radius: 0 0 12px 12px; text-align: center;'>
    <p style='color: #94a3b8; font-size: 12px; margin: 0;'>
      VetClinica by <strong>Ketra Solucoes Inteligentes</strong><br>
      Duvidas? <a href='mailto:suporte@ketra.com.br' style='color: #64748b;'>suporte@ketra.com.br</a>
    </p>
  </div>
</body>
</html>";

            var assunto = $"Bem-vindo ao VetClinica, {req.NomeDono}! Seus dados de acesso";
            var r = await _email.EnviarAsync(email, assunto, body);

            if (r.Ok)
            {
                Console.WriteLine($"[Trial] Email de boas-vindas enviado para {email} via {r.Provider}");
                return true;
            }

            Console.WriteLine($"[Trial] Falha ao enviar email ({r.Provider}): {r.Erro}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Trial] Erro ao enviar email: {ex.Message}");
            return false;
        }
    }
}
