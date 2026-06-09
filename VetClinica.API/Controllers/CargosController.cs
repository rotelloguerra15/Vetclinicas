using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cargos")]
public class CargosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public CargosController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lista = await _db.Cargos
            .Where(c => c.TenantId == _t.TenantId && c.Ativo)
            .OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.PodeReceituario, c.Ativo })
            .ToListAsync();
        return Ok(lista);
    }

    public record CargoCreate(string Nome, bool PodeReceituario);

    [HttpPost]
    public async Task<IActionResult> Criar(CargoCreate dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest(new { erro = "Nome obrigatório." });

        var duplicado = await _db.Cargos
            .AnyAsync(c => c.TenantId == _t.TenantId && c.Nome.ToLower() == dto.Nome.Trim().ToLower() && c.Ativo);
        if (duplicado)
            return BadRequest(new { erro = "Já existe um cargo com este nome." });

        var cargo = new Cargo
        {
            Id               = Guid.NewGuid(),
            TenantId         = _t.TenantId,
            Nome             = dto.Nome.Trim(),
            PodeReceituario  = dto.PodeReceituario,
            Ativo            = true,
            CriadoEm        = DateTime.UtcNow
        };
        _db.Cargos.Add(cargo);
        await _db.SaveChangesAsync();
        return Ok(new { cargo.Id, cargo.Nome, cargo.PodeReceituario });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, CargoCreate dto)
    {
        var cargo = await _db.Cargos
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (cargo == null) return NotFound();

        cargo.Nome            = dto.Nome.Trim();
        cargo.PodeReceituario = dto.PodeReceituario;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id)
    {
        var cargo = await _db.Cargos
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _t.TenantId);
        if (cargo == null) return NotFound();

        var emUso = await _db.Funcionarios.AnyAsync(f => f.CargoId == id);
        if (emUso)
        {
            cargo.Ativo = false;
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Cargo desativado (em uso por funcionários)." });
        }

        _db.Cargos.Remove(cargo);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
