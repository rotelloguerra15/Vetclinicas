using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/servicos")]
public class ServicosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public ServicosController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? categoria)
    {
        var q = _db.Servicos.Where(s => s.TenantId == _t.TenantId);
        if (!string.IsNullOrWhiteSpace(categoria))
            q = q.Where(s => s.Categoria == categoria);
        var items = await q.OrderBy(s => s.Nome)
            .Select(s => new ServicoDto(s.Id, s.Nome, s.Categoria, s.PrecoBase, s.DuracaoMin, s.Ativo, s.Icone ?? "🐾"))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(ServicoCreate dto)
    {
        var s = new Servico
        {
            Id        = Guid.NewGuid(),
            TenantId  = _t.TenantId,
            Nome      = dto.Nome,
            Categoria = dto.Categoria,
            Descricao = dto.Descricao,
            PrecoBase = dto.PrecoBase,
            DuracaoMin = dto.DuracaoMin,
            Icone     = dto.Icone ?? "🐾"
        };
        _db.Servicos.Add(s);
        await _db.SaveChangesAsync();
        return Ok(new { s.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(Guid id, ServicoCreate dto)
    {
        var s = await _db.Servicos.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (s == null) return NotFound();
        s.Nome      = dto.Nome;
        s.Categoria = dto.Categoria;
        s.Descricao = dto.Descricao;
        s.PrecoBase = dto.PrecoBase;
        s.DuracaoMin = dto.DuracaoMin;
        s.Icone     = dto.Icone ?? s.Icone ?? "🐾";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var s = await _db.Servicos.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (s == null) return NotFound();
        s.Ativo = !s.Ativo;
        await _db.SaveChangesAsync();
        return Ok(new { s.Ativo });
    }
}
