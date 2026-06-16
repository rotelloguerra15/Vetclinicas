using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Services.Certificado;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/certificado")]
public class CertificadoController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public CertificadoController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    // GET /api/certificado/status
    // Retorna info do certificado configurado (sem expor a chave)
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null || !p.CertAtivo || string.IsNullOrEmpty(p.CertPfxEncrypted))
            return Ok(new { configurado = false });

        return Ok(new {
            configurado   = true,
            tipo          = p.CertTipo ?? "a1",
            titular       = p.CertTitular,
            cpf           = p.CertCpf != null ? $"***{p.CertCpf[^3..]}" : null,
            validade      = p.CertValidade?.ToString("dd/MM/yyyy"),
            ativo         = p.CertAtivo,
            atualizadoEm  = p.CertAtualizadoEm?.ToString("dd/MM/yyyy HH:mm")
        });
    }

    // POST /api/certificado/upload-a1
    // Recebe o .pfx (multipart) + senha, criptografa e salva
    [HttpPost("upload-a1")]
    public async Task<IActionResult> UploadA1([FromForm] IFormFile arquivo, [FromForm] string senha)
    {
        if (arquivo == null || arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo .pfx nao informado." });
        if (arquivo.Length > 5 * 1024 * 1024)
            return BadRequest(new { erro = "Arquivo muito grande. Maximo 5MB." });
        if (string.IsNullOrWhiteSpace(senha))
            return BadRequest(new { erro = "Senha do certificado obrigatoria." });

        byte[] pfxBytes;
        using (var ms = new MemoryStream())
        {
            await arquivo.CopyToAsync(ms);
            pfxBytes = ms.ToArray();
        }

        // Valida o .pfx usando X509Certificate2 nativo do .NET 8
        string? titular = null, cpf = null;
        DateOnly? validade = null;
        try
        {
            using var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                pfxBytes, senha,
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);

            // Subject: CN=BARBARA FONSECA TATSCH:025083592XX, OU=...
            var subject = x509.Subject;
            var cnMatch = System.Text.RegularExpressions.Regex.Match(subject, @"CN=([^,]+)");
            if (cnMatch.Success)
            {
                var cn = cnMatch.Groups[1].Value.Trim();
                var parts = cn.Split(':');
                titular = parts[0].Trim();
                if (parts.Length > 1) cpf = parts[1].Trim();
            }

            validade = DateOnly.FromDateTime(x509.NotAfter);
        }
        catch
        {
            return BadRequest(new { erro = "Arquivo .pfx invalido ou senha incorreta." });
        }

        // Verifica validade
        if (validade.HasValue && validade.Value < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { erro = $"Certificado vencido em {validade.Value:dd/MM/yyyy}. Renove com sua certificadora." });

        // Criptografa e salva
        var pfxCriptografado   = CertificadoCryptoHelper.Criptografar(pfxBytes);
        var senhaCriptografada = CertificadoCryptoHelper.CriptografarTexto(senha);

        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null)
            return NotFound(new { erro = "Parametros nao encontrados. Acesse Configuracoes primeiro." });

        p.CertPfxEncrypted = pfxCriptografado;
        p.CertSenhaHash    = senhaCriptografada;
        p.CertTipo         = "a1";
        p.CertTitular      = titular;
        p.CertCpf          = cpf;
        p.CertValidade     = validade;
        p.CertAtivo        = true;
        p.CertAtualizadoEm = DateTime.UtcNow;
        p.AtualizadoEm     = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new {
            mensagem  = "Certificado A1 configurado com sucesso.",
            titular,
            validade  = validade?.ToString("dd/MM/yyyy"),
            tipo      = "a1"
        });
    }

    // PUT /api/certificado/ativar
    // Liga/desliga a assinatura automática
    [HttpPut("ativar")]
    public async Task<IActionResult> Ativar([FromBody] AtivarRequest req)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null) return NotFound();
        if (req.Ativo && string.IsNullOrEmpty(p.CertPfxEncrypted))
            return BadRequest(new { erro = "Nenhum certificado configurado. Faca o upload primeiro." });

        p.CertAtivo      = req.Ativo;
        p.AtualizadoEm   = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { ativo = p.CertAtivo });
    }

    // DELETE /api/certificado
    // Remove o certificado
    [HttpDelete]
    public async Task<IActionResult> Remover()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);
        if (p == null) return NotFound();

        p.CertPfxEncrypted = null;
        p.CertSenhaHash    = null;
        p.CertTipo         = null;
        p.CertTitular      = null;
        p.CertCpf          = null;
        p.CertValidade     = null;
        p.CertAtivo        = false;
        p.CertAtualizadoEm = DateTime.UtcNow;
        p.AtualizadoEm     = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Certificado removido." });
    }

    public record AtivarRequest(bool Ativo);
}
