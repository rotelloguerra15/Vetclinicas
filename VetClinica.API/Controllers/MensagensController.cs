using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/mensagens")]
public class MensagensController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public MensagensController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    // Lista todas as configs de mensagem do tenant
    [HttpGet("config")]
    public async Task<IActionResult> Configs()
    {
        var configs = await _db.ConfigMensagens
            .Where(c => c.TenantId == _t.TenantId)
            .OrderBy(c => c.Gatilho)
            .Select(c => new ConfigMensagemDto(c.Id, c.Gatilho, c.Canal, c.Ativo, c.HorasAntes, c.Template))
            .ToListAsync();
        return Ok(configs);
    }

    [HttpPut("config/{id}")]
    public async Task<IActionResult> Atualizar(Guid id, ConfigMensagemUpdate dto)
    {
        var c = await _db.ConfigMensagens.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (c == null) return NotFound();
        c.Canal = dto.Canal;
        c.Ativo = dto.Ativo;
        c.HorasAntes = dto.HorasAntes;
        c.Template = dto.Template;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
