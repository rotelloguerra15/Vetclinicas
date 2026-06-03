using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cadastros/vias")]
public class ViasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public ViasController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var vias = await _db.ViasAdministracao
            .Where(v => v.TenantId == _t.TenantId && v.Ativo)
            .OrderBy(v => v.Nome)
            .Select(v => new { v.Id, v.Nome })
            .ToListAsync();
        return Ok(vias);
    }

    public record ViaCreate(string Nome);

    [HttpPost]
    public async Task<IActionResult> Criar(ViaCreate dto)
    {
        var nome = dto.Nome.Trim();
        if (string.IsNullOrEmpty(nome))
            return BadRequest(new { erro = "Nome obrigatório." });

        var existe = await _db.ViasAdministracao
            .AnyAsync(v => v.TenantId == _t.TenantId && v.Nome.ToLower() == nome.ToLower() && v.Ativo);
        if (existe)
            return BadRequest(new { erro = "Já existe uma via com esse nome." });

        var via = new ViaAdministracao
        {
            Id        = Guid.NewGuid(),
            TenantId  = _t.TenantId,
            Nome      = nome,
            Ativo     = true,
            CriadoEm  = DateTime.UtcNow
        };
        _db.ViasAdministracao.Add(via);
        await _db.SaveChangesAsync();
        return Ok(new { via.Id, via.Nome });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var via = await _db.ViasAdministracao
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == _t.TenantId);
        if (via == null) return NotFound();

        // Verifica se já foi usada em algum receituário (prontuário tipo 'receita')
        // A via fica salva em texto no campo receituario — busca pelo nome
        var emUso = await _db.ProntuarioItens
            .AnyAsync(p => p.Tipo == "receita"
                && p.Receituario != null
                && p.Receituario.Contains($"via {via.Nome}"));

        if (emUso)
            return BadRequest(new { erro = $"A via \"{via.Nome}\" já foi usada em receituários e não pode ser excluída." });

        // Soft delete
        via.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
