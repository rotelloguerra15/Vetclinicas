using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cadastros/pelagens")]
public class PelagensController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public PelagensController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lista = await _db.Pelagens
            .Where(p => p.TenantId == _t.TenantId && p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new { p.Id, p.Nome })
            .ToListAsync();
        return Ok(lista);
    }

    public record PelagemCreate(string Nome);

    [HttpPost]
    public async Task<IActionResult> Criar(PelagemCreate dto)
    {
        var nome = dto.Nome.Trim();
        if (string.IsNullOrEmpty(nome))
            return BadRequest(new { erro = "Nome obrigatorio." });

        var existe = await _db.Pelagens
            .AnyAsync(p => p.TenantId == _t.TenantId && p.Nome.ToLower() == nome.ToLower() && p.Ativo);
        if (existe)
            return BadRequest(new { erro = "Ja existe uma pelagem com esse nome." });

        var pelagem = new Pelagem
        {
            Id       = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Nome     = nome,
            Ativo    = true,
            CriadoEm = DateTime.UtcNow
        };
        _db.Pelagens.Add(pelagem);
        await _db.SaveChangesAsync();
        return Ok(new { pelagem.Id, pelagem.Nome });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var p = await _db.Pelagens
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();
        p.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
