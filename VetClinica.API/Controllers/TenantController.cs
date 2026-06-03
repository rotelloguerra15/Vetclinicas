using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant")]
public class TenantController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public TenantController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet("branding")]
    public async Task<IActionResult> Branding()
    {
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == _t.TenantId);
        if (t == null) return NotFound();
        return Ok(new BrandingDto(t.Nome, t.Tagline, t.LogoUrl, t.CorPrimaria));
    }

    [HttpPut("branding")]
    public async Task<IActionResult> AtualizarBranding(BrandingUpdate dto)
    {
        if (_t.Papel != "owner" && _t.Papel != "admin")
            return StatusCode(403, new { erro = "Só o dono/admin pode alterar a marca" });

        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == _t.TenantId);
        if (t == null) return NotFound();
        t.Tagline = dto.Tagline;
        t.LogoUrl = dto.LogoUrl;
        t.CorPrimaria = dto.CorPrimaria;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Upload da logo da clínica como arquivo multipart.
    /// Converte para base64 data-URL e salva diretamente no banco (logo_url).
    /// Limite: 500 KB. Para logos maiores, use uma URL externa em PUT /branding.
    /// </summary>
    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLogo(IFormFile arquivo)
    {
        if (_t.Papel != "owner" && _t.Papel != "admin")
            return StatusCode(403, new { erro = "Só o dono/admin pode alterar a logo" });

        if (arquivo == null || arquivo.Length == 0)
            return BadRequest(new { erro = "Nenhum arquivo enviado." });

        const long maxBytes = 500 * 1024; // 500 KB
        if (arquivo.Length > maxBytes)
            return BadRequest(new { erro = "A logo deve ter no máximo 500 KB." });

        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var mimeMap = new Dictionary<string, string>
        {
            { ".png",  "image/png"  },
            { ".jpg",  "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".webp", "image/webp" },
            { ".svg",  "image/svg+xml" }
        };
        if (!mimeMap.TryGetValue(ext, out var mime))
            return BadRequest(new { erro = "Formato não suportado. Use PNG, JPG, WEBP ou SVG." });

        using var ms = new MemoryStream();
        await arquivo.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());
        var dataUrl = $"data:{mime};base64,{base64}";

        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == _t.TenantId);
        if (t == null) return NotFound();
        t.LogoUrl = dataUrl;
        await _db.SaveChangesAsync();

        return Ok(new { logoUrl = dataUrl });
    }

    /// <summary>Remove a logo da clínica.</summary>
    [HttpDelete("logo")]
    public async Task<IActionResult> RemoverLogo()
    {
        if (_t.Papel != "owner" && _t.Papel != "admin")
            return StatusCode(403, new { erro = "Sem permissão" });

        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == _t.TenantId);
        if (t == null) return NotFound();
        t.LogoUrl = null;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
