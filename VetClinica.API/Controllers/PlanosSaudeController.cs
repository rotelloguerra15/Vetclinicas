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
[Route("api/planos-saude")]
public class PlanosSaudeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;

    public PlanosSaudeController(AppDbContext db, TenantContext t)
    {
        _db = db;
        _t = t;
    }

    // ── Operadoras / Planos ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var planos = await _db.PlanosSaude
            .Where(p => p.TenantId == _t.TenantId && p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new PlanoSaudeDto(p.Id, p.Nome, p.Operadora, p.DescontoPercent, p.Obs, p.Ativo))
            .ToListAsync();
        return Ok(planos);
    }

    [HttpGet("todos")]
    public async Task<IActionResult> ListarTodos()
    {
        var planos = await _db.PlanosSaude
            .Where(p => p.TenantId == _t.TenantId)
            .OrderBy(p => p.Nome)
            .Select(p => new PlanoSaudeDto(p.Id, p.Nome, p.Operadora, p.DescontoPercent, p.Obs, p.Ativo))
            .ToListAsync();
        return Ok(planos);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(PlanoSaudeCreate dto)
    {
        var plano = new PlanoSaude
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Nome = dto.Nome,
            Operadora = dto.Operadora,
            DescontoPercent = dto.DescontoPercent,
            Obs = dto.Obs,
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };
        _db.PlanosSaude.Add(plano);
        await _db.SaveChangesAsync();
        return Ok(new { plano.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(Guid id, PlanoSaudeCreate dto)
    {
        var plano = await _db.PlanosSaude
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _t.TenantId);
        if (plano == null) return NotFound();

        plano.Nome = dto.Nome;
        plano.Operadora = dto.Operadora;
        plano.DescontoPercent = dto.DescontoPercent;
        plano.Obs = dto.Obs;
        plano.AtualizadoEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var plano = await _db.PlanosSaude
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _t.TenantId);
        if (plano == null) return NotFound();

        plano.Ativo = false;
        plano.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Vínculos Pet ─────────────────────────────────────────────────────────

    [HttpGet("pet/{petId}")]
    public async Task<IActionResult> PlanosDoPet(Guid petId)
    {
        var vinculos = await (
            from pp in _db.PetPlanos
            join p in _db.PlanosSaude on pp.PlanoId equals p.Id
            where pp.PetId == petId && pp.TenantId == _t.TenantId
            orderby pp.Ativo descending, pp.CriadoEm descending
            select new PetPlanoDto(
                pp.Id, pp.PetId, pp.PlanoId, p.Nome, p.Operadora,
                pp.NumCarteirinha, pp.Validade,
                pp.DescontoPercent ?? p.DescontoPercent,
                pp.Ativo)
        ).ToListAsync();
        return Ok(vinculos);
    }

    [HttpPost("pet/{petId}/vincular")]
    public async Task<IActionResult> VincularPet(Guid petId, VincularPlanoRequest dto)
    {
        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.Id == petId && p.TenantId == _t.TenantId);
        if (pet == null) return NotFound("Pet nao encontrado");

        var plano = await _db.PlanosSaude.FirstOrDefaultAsync(p => p.Id == dto.PlanoId && p.TenantId == _t.TenantId);
        if (plano == null) return NotFound("Plano nao encontrado");

        // Desativa vínculo anterior ativo (um pet = um plano ativo)
        var anterior = await _db.PetPlanos
            .Where(pp => pp.PetId == petId && pp.TenantId == _t.TenantId && pp.Ativo)
            .ToListAsync();
        anterior.ForEach(pp => { pp.Ativo = false; pp.AtualizadoEm = DateTime.UtcNow; });

        var vinculo = new PetPlano
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            PetId = petId,
            PlanoId = dto.PlanoId,
            NumCarteirinha = dto.NumCarteirinha,
            Validade = dto.Validade,
            DescontoPercent = dto.DescontoPercent,
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };
        _db.PetPlanos.Add(vinculo);

        // Atualiza flags legados no pet
        pet.TemPlanoSaude = true;
        pet.PlanoSaudeNome = plano.Nome;
        pet.PlanoSaudeCarteira = dto.NumCarteirinha;

        await _db.SaveChangesAsync();
        return Ok(new { vinculo.Id });
    }

    [HttpDelete("pet/{petId}/desvincular/{vinculoId}")]
    public async Task<IActionResult> DesvincularPet(Guid petId, Guid vinculoId)
    {
        var vinculo = await _db.PetPlanos
            .FirstOrDefaultAsync(pp => pp.Id == vinculoId && pp.PetId == petId && pp.TenantId == _t.TenantId);
        if (vinculo == null) return NotFound();

        vinculo.Ativo = false;
        vinculo.AtualizadoEm = DateTime.UtcNow;

        // Remove flags legados se era o plano ativo
        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.Id == petId);
        if (pet != null)
        {
            var temOutro = await _db.PetPlanos
                .AnyAsync(pp => pp.PetId == petId && pp.TenantId == _t.TenantId && pp.Ativo && pp.Id != vinculoId);
            if (!temOutro)
            {
                pet.TemPlanoSaude = false;
                pet.PlanoSaudeNome = null;
                pet.PlanoSaudeCarteira = null;
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Vínculos Tutor ────────────────────────────────────────────────────────

    [HttpGet("tutor/{tutorId}")]
    public async Task<IActionResult> PlanosDeTutor(Guid tutorId)
    {
        var vinculos = await (
            from tp in _db.TutorPlanos
            join p in _db.PlanosSaude on tp.PlanoId equals p.Id
            where tp.TutorId == tutorId && tp.TenantId == _t.TenantId
            orderby tp.Ativo descending, tp.CriadoEm descending
            select new TutorPlanoDto(
                tp.Id, tp.TutorId, tp.PlanoId, p.Nome, p.Operadora,
                tp.NumCarteirinha, tp.Validade,
                tp.DescontoPercent ?? p.DescontoPercent,
                tp.Ativo)
        ).ToListAsync();
        return Ok(vinculos);
    }

    [HttpPost("tutor/{tutorId}/vincular")]
    public async Task<IActionResult> VincularTutor(Guid tutorId, VincularPlanoRequest dto)
    {
        var tutor = await _db.Tutores.FirstOrDefaultAsync(t => t.Id == tutorId && t.TenantId == _t.TenantId);
        if (tutor == null) return NotFound("Tutor nao encontrado");

        var plano = await _db.PlanosSaude.FirstOrDefaultAsync(p => p.Id == dto.PlanoId && p.TenantId == _t.TenantId);
        if (plano == null) return NotFound("Plano nao encontrado");

        var anterior = await _db.TutorPlanos
            .Where(tp => tp.TutorId == tutorId && tp.TenantId == _t.TenantId && tp.Ativo)
            .ToListAsync();
        anterior.ForEach(tp => { tp.Ativo = false; tp.AtualizadoEm = DateTime.UtcNow; });

        var vinculo = new TutorPlano
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            TutorId = tutorId,
            PlanoId = dto.PlanoId,
            NumCarteirinha = dto.NumCarteirinha,
            Validade = dto.Validade,
            DescontoPercent = dto.DescontoPercent,
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };
        _db.TutorPlanos.Add(vinculo);
        await _db.SaveChangesAsync();
        return Ok(new { vinculo.Id });
    }

    [HttpDelete("tutor/{tutorId}/desvincular/{vinculoId}")]
    public async Task<IActionResult> DesvincularTutor(Guid tutorId, Guid vinculoId)
    {
        var vinculo = await _db.TutorPlanos
            .FirstOrDefaultAsync(tp => tp.Id == vinculoId && tp.TutorId == tutorId && tp.TenantId == _t.TenantId);
        if (vinculo == null) return NotFound();

        vinculo.Ativo = false;
        vinculo.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Endpoint de desconto (usado pelo PDV e OS) ────────────────────────────
    // GET /api/planos-saude/desconto?petId=...  OU  ?tutorId=...
    // Retorna o desconto efetivo a aplicar (pet tem prioridade sobre tutor)

    [HttpGet("desconto")]
    public async Task<IActionResult> ObterDesconto([FromQuery] Guid? petId, [FromQuery] Guid? tutorId)
    {
        if (petId.HasValue)
        {
            var pp = await (
                from v in _db.PetPlanos
                join p in _db.PlanosSaude on v.PlanoId equals p.Id
                where v.PetId == petId.Value && v.TenantId == _t.TenantId && v.Ativo
                select new { v.PlanoId, PlanoNome = p.Nome, Desconto = v.DescontoPercent ?? p.DescontoPercent, v.Validade }
            ).FirstOrDefaultAsync();

            if (pp != null)
                return Ok(new DescontoPlanoResponse(pp.PlanoId, pp.PlanoNome, pp.Desconto, pp.Validade));
        }

        if (tutorId.HasValue)
        {
            var tp = await (
                from v in _db.TutorPlanos
                join p in _db.PlanosSaude on v.PlanoId equals p.Id
                where v.TutorId == tutorId.Value && v.TenantId == _t.TenantId && v.Ativo
                select new { v.PlanoId, PlanoNome = p.Nome, Desconto = v.DescontoPercent ?? p.DescontoPercent, v.Validade }
            ).FirstOrDefaultAsync();

            if (tp != null)
                return Ok(new DescontoPlanoResponse(tp.PlanoId, tp.PlanoNome, tp.Desconto, tp.Validade));
        }

        // Sem plano ativo
        return Ok(new DescontoPlanoResponse(null, null, 0, null));
    }
}
