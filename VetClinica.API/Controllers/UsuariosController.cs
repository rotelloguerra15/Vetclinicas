using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public UsuariosController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var users = await _db.Users
            .Where(u => u.TenantId == _t.TenantId)
            .OrderBy(u => u.Nome)
            .Select(u => new { u.Id, u.Nome, u.Email, u.Papel, u.Ativo })
            .ToListAsync();
        return Ok(users);
    }

    public record UsuarioCreate(string Nome, string Email, string Papel, string Senha);
    public record UsuarioEdit(string Nome, string Email, string Papel);

    [HttpPost]
    public async Task<IActionResult> Criar(UsuarioCreate dto)
    {
        if (_t.Papel != "owner" && _t.Papel != "admin")
            return StatusCode(403, new { erro = "Apenas dono/admin pode criar usuários" });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { erro = "Email já cadastrado" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Nome = dto.Nome,
            Email = dto.Email,
            Papel = dto.Papel,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha, 12),
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { user.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(Guid id, UsuarioEdit dto)
    {
        if (_t.Papel != "owner" && _t.Papel != "admin")
            return StatusCode(403, new { erro = "Apenas dono/admin pode editar usuários" });

        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (u == null) return NotFound();
        u.Nome = dto.Nome; u.Email = dto.Email; u.Papel = dto.Papel;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/desativar")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        if (_t.Papel != "owner" && _t.Papel != "admin")
            return StatusCode(403, new { erro = "Apenas dono/admin" });

        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (u == null) return NotFound();
        u.Ativo = !u.Ativo;
        await _db.SaveChangesAsync();
        return Ok(new { u.Ativo });
    }
}
