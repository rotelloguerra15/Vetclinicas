using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Models;
using VetClinica.API.Services.RH;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/rh/funcionarios")]
public class FuncionariosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    private readonly FuncionarioService _service;

    public FuncionariosController(AppDbContext db, TenantContext t, FuncionarioService service)
    {
        _db = db; _t = t; _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lista = await _db.Funcionarios
            .Where(f => f.TenantId == _t.TenantId)
            .OrderBy(f => f.Nome)
            .Select(f => new FuncionarioDto(
                f.Id, f.Nome, f.Cargo, f.Crmv, f.RegistroMapa, f.Telefone, f.Email,
                f.Salario, f.PercentualComissao, f.Status, f.DataAdmissao))
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id)
    {
        var f = await _db.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == _t.TenantId);
        return f == null ? NotFound() : Ok(f);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(FuncionarioCreate dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest(new { erro = "Nome obrigatório" });

        var func = new Funcionario
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Nome = dto.Nome.Trim(),
            Cpf = dto.Cpf, Rg = dto.Rg,
            DataNascimento = dto.DataNascimento,
            Logradouro = dto.Logradouro, Numero = dto.Numero,
            Complemento = dto.Complemento, Bairro = dto.Bairro,
            Cidade = dto.Cidade, Estado = dto.Estado, Cep = dto.Cep,
            Telefone = dto.Telefone, Email = dto.Email,
            Cargo = dto.Cargo, Crmv = dto.Crmv, RegistroMapa = dto.RegistroMapa, DataAdmissao = dto.DataAdmissao,
            Salario = dto.Salario,
            PercentualComissao = dto.PercentualComissao,
            Status = dto.Status,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        _db.Funcionarios.Add(func);
        await _db.SaveChangesAsync();
        return Ok(new { func.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, FuncionarioCreate dto)
    {
        var func = await _db.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == _t.TenantId);
        if (func == null) return NotFound();

        func.Nome = dto.Nome.Trim();
        func.Cpf = dto.Cpf; func.Rg = dto.Rg;
        func.DataNascimento = dto.DataNascimento;
        func.Logradouro = dto.Logradouro; func.Numero = dto.Numero;
        func.Complemento = dto.Complemento; func.Bairro = dto.Bairro;
        func.Cidade = dto.Cidade; func.Estado = dto.Estado; func.Cep = dto.Cep;
        func.Telefone = dto.Telefone; func.Email = dto.Email;
        func.Cargo = dto.Cargo;
        func.Crmv = dto.Crmv;
        func.RegistroMapa = dto.RegistroMapa; func.DataAdmissao = dto.DataAdmissao;
        func.Salario = dto.Salario;
        func.PercentualComissao = dto.PercentualComissao;
        func.Status = dto.Status;
        func.AtualizadoEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, FuncionarioStatusUpdate dto)
    {
        var validos = new[] { "trabalhando", "ferias", "demitido" };
        if (!validos.Contains(dto.Status))
            return BadRequest(new { erro = "Status inválido. Use: trabalhando, ferias, demitido" });

        var func = await _db.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == _t.TenantId);
        if (func == null) return NotFound();

        func.Status = dto.Status;
        func.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
