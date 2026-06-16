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
[Route("api/pets")]
public class PetsController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public PetsController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca, [FromQuery] string? especie,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.Pets.Include(p => p.Tutor)
            .Where(p => p.TenantId == _t.TenantId && p.Ativo);
        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(p => p.Nome.ToLower().Contains(busca.ToLower()));
        if (!string.IsNullOrWhiteSpace(especie))
            q = q.Where(p => p.Especie == especie);

        var total = await q.CountAsync();
        var raw = await q.OrderBy(p => p.Nome)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var items = raw.Select(p => new PetDto(
            p.Id, p.TutorId, p.Tutor!.Nome, p.Nome, p.Especie,
            p.Raca, p.Sexo, p.DataNascimento, p.PesoKg, p.FotoUrl,
            CalcularIdade(p.DataNascimento),
            p.TemMicrochip, p.MicrochipNum,
            p.TemPlanoSaude, p.PlanoSaudeNome, p.PlanoSaudeCarteira)).ToList();

        return Ok(new PagedResult<PetDto>(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalhe(Guid id)
    {
        var p = await _db.Pets.Include(x => x.Tutor)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();
        return Ok(new
        {
            p.Id, p.TutorId,
            TutorNome = p.Tutor?.Nome,
            TutorTelefone = p.Tutor?.Telefone,
            p.Nome, p.Especie, p.Raca, p.Sexo, p.DataNascimento,
            IdadeFormatada = CalcularIdade(p.DataNascimento),
            p.Pelagem, p.PesoKg, p.Castrado, p.Cor, p.FotoUrl, p.Obs,
            // microchip
            p.TemMicrochip, p.MicrochipNum,
            // plano de saúde
            p.TemPlanoSaude, p.PlanoSaudeNome, p.PlanoSaudeCarteira
        });
    }

    [HttpPost]
    public async Task<IActionResult> Criar(PetCreate dto)
    {
        var pet = new Pet
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            TutorId = dto.TutorId,
            Nome = dto.Nome,
            Especie = dto.Especie,
            Raca = dto.Raca,
            Sexo = dto.Sexo,
            DataNascimento = dto.DataNascimento,
            Pelagem = dto.Pelagem,
            PesoKg = dto.PesoKg,
            Castrado = dto.Castrado,
            Cor = dto.Cor,
            Obs = dto.Obs,
            TemMicrochip = dto.TemMicrochip,
            MicrochipNum = dto.TemMicrochip ? dto.MicrochipNum : null,
            TemPlanoSaude = dto.TemPlanoSaude,
            PlanoSaudeNome = dto.TemPlanoSaude ? dto.PlanoSaudeNome : null,
            PlanoSaudeCarteira = dto.TemPlanoSaude ? dto.PlanoSaudeCarteira : null,
            CriadoEm = DateTime.UtcNow
        };
        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();
        return Ok(new { pet.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(Guid id, PetCreate dto)
    {
        var p = await _db.Pets.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (p == null) return NotFound();
        p.Nome = dto.Nome; p.Especie = dto.Especie; p.Raca = dto.Raca; p.Sexo = dto.Sexo;
        p.DataNascimento = dto.DataNascimento; p.Pelagem = dto.Pelagem; p.PesoKg = dto.PesoKg;
        p.Castrado = dto.Castrado; p.Cor = dto.Cor; p.Obs = dto.Obs;
        p.TemMicrochip = dto.TemMicrochip;
        p.MicrochipNum = dto.TemMicrochip ? dto.MicrochipNum : null;
        p.TemPlanoSaude = dto.TemPlanoSaude;
        p.PlanoSaudeNome = dto.TemPlanoSaude ? dto.PlanoSaudeNome : null;
        p.PlanoSaudeCarteira = dto.TemPlanoSaude ? dto.PlanoSaudeCarteira : null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/prontuario")]
    public async Task<IActionResult> Prontuario(Guid id)
    {
        var itens = await _db.ProntuarioItens
            .Where(x => x.PetId == id)
            .OrderByDescending(x => x.Data)
            .Select(x => new ProntuarioDto(x.Id, x.Data, x.Tipo, x.Titulo, x.Motivo, x.Descricao, x.Receituario, x.AnexoUrl))
            .ToListAsync();
        return Ok(itens);
    }

    [HttpGet("{id}/vacinas")]
    public async Task<IActionResult> Vacinas(Guid id)
    {
        var vacinas = await _db.Vacinacoes
            .Where(v => v.PetId == id)
            .OrderByDescending(v => v.DataAplicacao)
            .Select(v => new VacinaDto(v.Id, v.Vacina, v.Fabricante, v.DataAplicacao, v.ProximaDose))
            .ToListAsync();
        return Ok(vacinas);
    }

    [HttpGet("{id}/agendamentos")]
    public async Task<IActionResult> Agendamentos(Guid id)
    {
        var ags = await _db.Agendamentos
            .Where(a => a.PetId == id)
            .OrderByDescending(a => a.DataHora)
            .Select(a => new AgendamentoDto(a.Id, a.PetId, a.Pet!.Nome, a.Tipo,
                a.DataHora, a.DuracaoMin, a.Status, a.Origem, a.Obs))
            .ToListAsync();
        return Ok(ags);
    }

    // ─── helper: calcula idade de forma legível ───────────────────────────────
    public static string? CalcularIdade(DateOnly? dataNasc)
    {
        if (dataNasc == null) return null;
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var anos = hoje.Year - dataNasc.Value.Year;
        var meses = hoje.Month - dataNasc.Value.Month;
        var dias = hoje.Day - dataNasc.Value.Day;
        if (dias < 0) { meses--; dias += DateTime.DaysInMonth(hoje.Year, hoje.Month == 1 ? 12 : hoje.Month - 1); }
        if (meses < 0) { anos--; meses += 12; }
        if (anos > 0) return $"{anos} ano{(anos > 1 ? "s" : "")}{(meses > 0 ? $", {meses} mês(es)" : "")}";
        if (meses > 0) return $"{meses} mês(es){(dias > 0 ? $", {dias} dia{(dias > 1 ? "s" : "")}" : "")}";
        return $"{dias} dia{(dias > 1 ? "s" : "")}";
    }
}
