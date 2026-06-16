using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public AdminController(PlatformDbContext platform, TenantContext t, ProvisionamentoService prov)
    { _platform = platform; _t = t; _prov = prov; }

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
                Suspenso    = t.SuspensoEm != null,
                t.Email,
                t.SchemaName,
                t.CriadoEm
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
}
