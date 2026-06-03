using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

// Painel do super-admin da plataforma (Rotello).
// Todos os endpoints exigem token de plataforma (claim platform_admin=true).
[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    private readonly ProvisionamentoService _prov;
    public AdminController(AppDbContext db, TenantContext t, ProvisionamentoService prov)
    { _db = db; _t = t; _prov = prov; }

    private IActionResult? Guard() =>
        _t.IsPlatformAdmin ? null : StatusCode(403, new { erro = "Acesso restrito ao administrador da plataforma" });

    // Lista todas as clínicas (tenants) da plataforma
    [HttpGet("clinicas")]
    public async Task<IActionResult> Clinicas()
    {
        var g = Guard(); if (g != null) return g;

        var clinicas = await _db.Tenants
            .OrderByDescending(t => t.CriadoEm)
            .Select(t => new ClinicaListItem(
                t.Id, t.Nome, t.Plano, t.Ativo, t.SuspensoEm != null,
                _db.Users.Count(u => u.TenantId == t.Id && u.Ativo),
                _db.Pets.Count(p => p.TenantId == t.Id && p.Ativo),
                t.CriadoEm))
            .ToListAsync();
        return Ok(clinicas);
    }

    // Cria uma clínica nova "chave na mão"
    [HttpPost("clinicas")]
    public async Task<IActionResult> CriarClinica(NovaClinicaRequest req)
    {
        var g = Guard(); if (g != null) return g;

        var jaExiste = await _db.Users.AnyAsync(u => u.Email == req.EmailDono);
        if (jaExiste) return BadRequest(new { erro = "Já existe um usuário com esse email" });

        var r = await _prov.CriarClinica(req.NomeClinica, req.Plano, req.NomeDono,
            req.EmailDono, req.Telefone, req.Tagline);

        return Ok(new NovaClinicaResponse(r.TenantId, r.LoginEmail, r.SenhaTemporaria));
    }

    // Suspende uma clínica (ex.: inadimplência) — bloqueia o login dos usuários dela
    [HttpPut("clinicas/{id}/suspender")]
    public async Task<IActionResult> Suspender(Guid id)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.SuspensoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("clinicas/{id}/reativar")]
    public async Task<IActionResult> Reativar(Guid id)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.SuspensoEm = null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("clinicas/{id}/plano")]
    public async Task<IActionResult> MudarPlano(Guid id, MudarPlanoRequest req)
    {
        var g = Guard(); if (g != null) return g;
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.Plano = req.Plano;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Métricas gerais da plataforma
    [HttpGet("metricas")]
    public async Task<IActionResult> Metricas()
    {
        var g = Guard(); if (g != null) return g;
        return Ok(new
        {
            totalClinicas = await _db.Tenants.CountAsync(),
            clinicasAtivas = await _db.Tenants.CountAsync(t => t.Ativo && t.SuspensoEm == null),
            clinicasSuspensas = await _db.Tenants.CountAsync(t => t.SuspensoEm != null),
            totalPets = await _db.Pets.CountAsync(p => p.Ativo),
            porPlano = await _db.Tenants.GroupBy(t => t.Plano)
                .Select(grp => new { plano = grp.Key, qtd = grp.Count() }).ToListAsync()
        });
    }
}
