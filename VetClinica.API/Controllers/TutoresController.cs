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
[Route("api/tutores")]
public class TutoresController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public TutoresController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.Tutores.Where(t => t.TenantId == _t.TenantId && t.Ativo);
        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(t => t.Nome.ToLower().Contains(busca.ToLower())
                          || (t.Telefone != null && t.Telefone.Contains(busca)));

        var total = await q.CountAsync();
        var items = await q.OrderBy(t => t.Nome)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TutorDto(
                t.Id, t.Nome, t.CpfCnpj, t.Telefone, t.Email, t.Endereco, t.Pontos,
                t.Pets.Count(p => p.Ativo),
                t.Logradouro, t.NumEnd, t.Complemento, t.Bairro, t.Cidade, t.Estado, t.Cep))
            .ToListAsync();

        return Ok(new PagedResult<TutorDto>(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalhe(Guid id)
    {
        var t = await _db.Tutores.Include(x => x.Pets)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (t == null) return NotFound();
        return Ok(new
        {
            t.Id, t.Nome, t.CpfCnpj, t.Telefone, t.Email,
            t.Endereco,
            t.Logradouro, t.NumEnd, t.Complemento, t.Bairro, t.Cidade, t.Estado, t.Cep,
            t.Obs, t.Pontos, t.AniversarioDia, t.AniversarioMes,
            Pets = t.Pets.Where(p => p.Ativo).Select(p => new
            {
                p.Id, p.Nome, p.Especie, p.Raca, p.Sexo, p.PesoKg, p.FotoUrl
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Criar(TutorCreate dto)
    {
        var tutor = new Tutor
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Nome = dto.Nome,
            CpfCnpj = dto.CpfCnpj,
            Telefone = dto.Telefone,
            Email = dto.Email,
            Obs = dto.Obs,
            AniversarioDia = dto.AniversarioDia,
            AniversarioMes = dto.AniversarioMes,
            Logradouro = dto.Logradouro,
            NumEnd = dto.NumEnd,
            Complemento = dto.Complemento,
            Bairro = dto.Bairro,
            Cidade = dto.Cidade,
            Estado = dto.Estado,
            Cep = dto.Cep,
            // monta campo legado para compatibilidade
            Endereco = MontarEnderecoLegado(dto),
            CriadoEm = DateTime.UtcNow
        };
        _db.Tutores.Add(tutor);
        await _db.SaveChangesAsync();
        return Ok(new { tutor.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(Guid id, TutorCreate dto)
    {
        var t = await _db.Tutores.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (t == null) return NotFound();
        t.Nome = dto.Nome; t.CpfCnpj = dto.CpfCnpj; t.Telefone = dto.Telefone;
        t.Email = dto.Email; t.Obs = dto.Obs;
        t.AniversarioDia = dto.AniversarioDia; t.AniversarioMes = dto.AniversarioMes;
        t.Logradouro = dto.Logradouro; t.NumEnd = dto.NumEnd; t.Complemento = dto.Complemento;
        t.Bairro = dto.Bairro; t.Cidade = dto.Cidade; t.Estado = dto.Estado; t.Cep = dto.Cep;
        t.Endereco = MontarEnderecoLegado(dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/pets")]
    public async Task<IActionResult> Pets(Guid id)
    {
        var pets = await _db.Pets
            .Where(p => p.TutorId == id && p.TenantId == _t.TenantId && p.Ativo)
            .Select(p => new PetDto(
                p.Id, p.TutorId, p.Tutor!.Nome, p.Nome, p.Especie,
                p.Raca, p.Sexo, p.DataNascimento, p.PesoKg, p.FotoUrl,
                CalcularIdade(p.DataNascimento),
                p.TemMicrochip, p.MicrochipNum,
                p.TemPlanoSaude, p.PlanoSaudeNome, p.PlanoSaudeCarteira))
            .ToListAsync();
        return Ok(pets);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static string? MontarEnderecoLegado(TutorCreate dto)
    {
        var partes = new[] { dto.Logradouro, dto.NumEnd, dto.Complemento, dto.Bairro, dto.Cidade, dto.Estado }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var endereco = string.Join(", ", partes);
        return string.IsNullOrWhiteSpace(endereco) ? null : endereco;
    }

    private static string? CalcularIdade(DateOnly? dataNasc)
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
