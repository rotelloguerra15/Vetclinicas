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
[Route("api/vacinas")]
public class VacinacoesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public VacinacoesController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpPost]
    public async Task<IActionResult> Criar(VacinaCreate dto)
    {
        var v = new Vacinacao
        {
            Id = Guid.NewGuid(),
            PetId = dto.PetId,
            UserId = _t.UserId,
            Vacina = dto.Vacina,
            Fabricante = dto.Fabricante,
            Lote = dto.Lote,
            DataAplicacao = dto.DataAplicacao,
            ProximaDose = dto.ProximaDose,
            Obs = dto.Obs,
            CriadoEm = DateTime.UtcNow
        };
        _db.Vacinacoes.Add(v);
        await _db.SaveChangesAsync();
        return Ok(v);
    }

    // Vacinas com próxima dose vencendo nos próximos N dias
    [HttpGet("vencendo")]
    public async Task<IActionResult> Vencendo([FromQuery] int dias = 30)
    {
        var limite = DateOnly.FromDateTime(DateTime.Today.AddDays(dias));
        var vacinas = await _db.Vacinacoes
            .Where(v => v.ProximaDose != null && v.ProximaDose <= limite
                     && v.ProximaDose >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(v => v.ProximaDose)
            .ToListAsync();
        return Ok(vacinas);
    }
}
