using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/fornecedores")]
public class FornecedoresController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    public FornecedoresController(TenantDbContextFactory factory, TenantContext t)
    { _db = factory.Create(); _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.Fornecedores.Where(f => f.TenantId == _t.TenantId && f.Ativo);
        if (!string.IsNullOrEmpty(busca))
            q = q.Where(f => f.Nome.ToLower().Contains(busca.ToLower())
                          || (f.Cnpj != null && f.Cnpj.Contains(busca)));
        var total = await q.CountAsync();
        var items = await q.OrderBy(f => f.Nome)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(f => new { f.Id, f.Nome, f.Cnpj, f.Telefone, f.Email, f.Contato, f.Cidade, f.Estado })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalhe(Guid id)
    {
        var f = await _db.Fornecedores.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (f == null) return NotFound();
        return Ok(f);
    }

    public record FornecedorCreate(
        string Nome, string? Codigo, string? Cnpj, string? Telefone, string? Email, string? Contato,
        string? Logradouro, string? NumEnd, string? Bairro, string? Cidade, string? Estado, string? Cep,
        string? Obs);

    [HttpPost]
    public async Task<IActionResult> Criar(FornecedorCreate dto)
    {
        var f = new Fornecedor
        {
            Id = Guid.NewGuid(), TenantId = _t.TenantId,
            Nome = dto.Nome, Codigo = dto.Codigo, Cnpj = dto.Cnpj, Telefone = dto.Telefone,
            Email = dto.Email, Contato = dto.Contato,
            Logradouro = dto.Logradouro, NumEnd = dto.NumEnd, Bairro = dto.Bairro,
            Cidade = dto.Cidade, Estado = dto.Estado, Cep = dto.Cep, Obs = dto.Obs,
            CriadoEm = DateTime.UtcNow
        };
        _db.Fornecedores.Add(f);
        await _db.SaveChangesAsync();
        return Ok(new { f.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(Guid id, FornecedorCreate dto)
    {
        var f = await _db.Fornecedores.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (f == null) return NotFound();
        f.Nome = dto.Nome; f.Codigo = dto.Codigo; f.Cnpj = dto.Cnpj; f.Telefone = dto.Telefone;
        f.Email = dto.Email; f.Contato = dto.Contato;
        f.Logradouro = dto.Logradouro; f.NumEnd = dto.NumEnd; f.Bairro = dto.Bairro;
        f.Cidade = dto.Cidade; f.Estado = dto.Estado; f.Cep = dto.Cep; f.Obs = dto.Obs;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(Guid id)
    {
        var f = await _db.Fornecedores.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (f == null) return NotFound();
        var temPedidos = await _db.PedidosCompra.AnyAsync(p => p.FornecedorId == id);
        if (temPedidos) return BadRequest(new { erro = "Fornecedor tem pedidos e nao pode ser removido." });
        f.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
