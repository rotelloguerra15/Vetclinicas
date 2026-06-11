using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cadastros/racas")]
public class RacasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public RacasController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? especie)
    {
        var q = _db.Racas.Where(r => r.TenantId == _t.TenantId && r.Ativo);
        if (!string.IsNullOrWhiteSpace(especie))
            q = q.Where(r => r.Especie == especie);
        var lista = await q.OrderBy(r => r.Nome)
            .Select(r => new { r.Id, r.Nome, r.Especie })
            .ToListAsync();
        return Ok(lista);
    }

    public record RacaCreate(string Nome, string Especie);

    [HttpPost]
    public async Task<IActionResult> Criar(RacaCreate dto)
    {
        var nome = dto.Nome.Trim();
        if (string.IsNullOrEmpty(nome))
            return BadRequest(new { erro = "Nome obrigatorio." });

        var existe = await _db.Racas
            .AnyAsync(r => r.TenantId == _t.TenantId && r.Nome.ToLower() == nome.ToLower()
                        && r.Especie == dto.Especie && r.Ativo);
        if (existe)
            return BadRequest(new { erro = "Raca ja cadastrada para esta especie." });

        var raca = new Raca
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Nome = nome,
            Especie = dto.Especie,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
        _db.Racas.Add(raca);
        await _db.SaveChangesAsync();
        return Ok(new { raca.Id, raca.Nome, raca.Especie });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var r = await _db.Racas
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (r == null) return NotFound();
        r.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
