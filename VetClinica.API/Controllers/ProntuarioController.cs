using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/prontuario")]
public class ProntuarioController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public ProntuarioController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpPost]
    public async Task<IActionResult> Criar(ProntuarioCreate dto)
    {
        var item = new ProntuarioItem
        {
            Id = Guid.NewGuid(),
            PetId = dto.PetId,
            UserId = _t.UserId,
            Data = dto.Data ?? DateTime.UtcNow,
            Tipo = dto.Tipo,
            Titulo = dto.Titulo,
            Motivo = dto.Motivo,
            Descricao = dto.Descricao,
            Receituario = dto.Receituario,
            AnexoUrl = dto.AnexoUrl,
            CriadoEm = DateTime.UtcNow
        };
        _db.ProntuarioItens.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }
}
