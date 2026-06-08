using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/parametros")]
public class ParametrosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public ParametrosController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null)
        {
            // Cria com defaults se nao existir
            p = new ParametrosSistema
            {
                Id          = Guid.NewGuid(),
                TenantId    = _t.TenantId,
                ComissaoOsAtivo  = true,
                ComissaoPdvAtivo = false,
                CriadoEm    = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };
            _db.ParametrosSistema.Add(p);
            await _db.SaveChangesAsync();
        }

        return Ok(new {
            p.Id,
            p.ComissaoOsAtivo,
            p.ComissaoPdvAtivo
        });
    }

    public record ParametrosUpdate(bool ComissaoOsAtivo, bool ComissaoPdvAtivo);

    [HttpPut]
    public async Task<IActionResult> Atualizar(ParametrosUpdate dto)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null)
        {
            p = new ParametrosSistema
            {
                Id       = Guid.NewGuid(),
                TenantId = _t.TenantId,
                CriadoEm = DateTime.UtcNow
            };
            _db.ParametrosSistema.Add(p);
        }

        p.ComissaoOsAtivo  = dto.ComissaoOsAtivo;
        p.ComissaoPdvAtivo = dto.ComissaoPdvAtivo;
        p.AtualizadoEm     = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
