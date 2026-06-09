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
            .Include(f => f.CargoObj)
            .Include(f => f.Usuario)
            .Where(f => f.TenantId == _t.TenantId)
            .OrderBy(f => f.Nome)
            .Select(f => new {
                f.Id, f.Nome, f.Codigo, f.Telefone, f.Email,
                f.Crmv, f.RegistroMapa,
                f.Salario, f.PercentualComissao, f.Status, f.DataAdmissao,
                // cargo
                CargoId   = f.CargoId,
                CargoNome = f.CargoObj != null ? f.CargoObj.Nome : f.Cargo,
                PodeReceituario = f.CargoObj != null ? f.CargoObj.PodeReceituario : f.AssinaReceituario,
                // usuario vinculado
                UsuarioId   = f.UsuarioId,
                UsuarioNome = f.Usuario != null ? f.Usuario.Nome : null,
                UsuarioEmail= f.Usuario != null ? f.Usuario.Email : null,
            })
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id)
    {
        var f = await _db.Funcionarios
            .Include(f => f.CargoObj)
            .Include(f => f.Usuario)
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == _t.TenantId);
        return f == null ? NotFound() : Ok(f);
    }

    public record FuncionarioCreateV2(
        string Nome,
        string? Codigo,
        string? Cpf, string? Rg,
        DateOnly? DataNascimento,
        string? Logradouro, string? Numero, string? Complemento,
        string? Bairro, string? Cidade, string? Estado, string? Cep,
        string? Telefone, string? Email,
        Guid? CargoId,          // novo — id do cargo
        string? Crmv,
        string? RegistroMapa,
        DateOnly? DataAdmissao,
        decimal Salario,
        decimal PercentualComissao,
        string Status,
        Guid? UsuarioId         // novo — vínculo com login
    );

    [HttpPost]
    public async Task<IActionResult> Criar(FuncionarioCreateV2 dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest(new { erro = "Nome obrigatório" });

        // Valida cargo
        Cargo? cargo = null;
        if (dto.CargoId.HasValue)
        {
            cargo = await _db.Cargos.FirstOrDefaultAsync(c => c.Id == dto.CargoId.Value && c.TenantId == _t.TenantId);
            if (cargo == null) return BadRequest(new { erro = "Cargo não encontrado." });
        }

        var func = new Funcionario
        {
            Id                 = Guid.NewGuid(),
            TenantId           = _t.TenantId,
            Nome               = dto.Nome.Trim(),
            Codigo             = dto.Codigo,
            Cpf                = dto.Cpf, Rg = dto.Rg,
            DataNascimento     = dto.DataNascimento,
            Logradouro         = dto.Logradouro, Numero = dto.Numero,
            Complemento        = dto.Complemento, Bairro = dto.Bairro,
            Cidade             = dto.Cidade, Estado = dto.Estado, Cep = dto.Cep,
            Telefone           = dto.Telefone, Email = dto.Email,
            Cargo              = cargo?.Nome,    // mantém campo texto por compatibilidade
            CargoId            = dto.CargoId,
            UsuarioId          = dto.UsuarioId,
            Crmv               = dto.Crmv,
            RegistroMapa       = dto.RegistroMapa,
            DataAdmissao       = dto.DataAdmissao,
            Salario            = dto.Salario,
            PercentualComissao = dto.PercentualComissao,
            Status             = dto.Status,
            AssinaReceituario  = cargo?.PodeReceituario ?? false,
            CriadoEm          = DateTime.UtcNow,
            AtualizadoEm      = DateTime.UtcNow
        };

        _db.Funcionarios.Add(func);
        await _db.SaveChangesAsync();
        return Ok(new { func.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, FuncionarioCreateV2 dto)
    {
        var func = await _db.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == _t.TenantId);
        if (func == null) return NotFound();

        Cargo? cargo = null;
        if (dto.CargoId.HasValue)
        {
            cargo = await _db.Cargos.FirstOrDefaultAsync(c => c.Id == dto.CargoId.Value && c.TenantId == _t.TenantId);
            if (cargo == null) return BadRequest(new { erro = "Cargo não encontrado." });
        }

        func.Codigo             = dto.Codigo;
        func.Nome               = dto.Nome.Trim();
        func.Cpf                = dto.Cpf; func.Rg = dto.Rg;
        func.DataNascimento     = dto.DataNascimento;
        func.Logradouro         = dto.Logradouro; func.Numero = dto.Numero;
        func.Complemento        = dto.Complemento; func.Bairro = dto.Bairro;
        func.Cidade             = dto.Cidade; func.Estado = dto.Estado; func.Cep = dto.Cep;
        func.Telefone           = dto.Telefone; func.Email = dto.Email;
        func.Cargo              = cargo?.Nome ?? func.Cargo;
        func.CargoId            = dto.CargoId;
        func.UsuarioId          = dto.UsuarioId;
        func.Crmv               = dto.Crmv;
        func.RegistroMapa       = dto.RegistroMapa;
        func.DataAdmissao       = dto.DataAdmissao;
        func.Salario            = dto.Salario;
        func.PercentualComissao = dto.PercentualComissao;
        func.Status             = dto.Status;
        func.AssinaReceituario  = cargo?.PodeReceituario ?? func.AssinaReceituario;
        func.AtualizadoEm      = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, FuncionarioStatusUpdate dto)
    {
        var validos = new[] { "trabalhando", "ferias", "demitido" };
        if (!validos.Contains(dto.Status))
            return BadRequest(new { erro = "Status inválido." });

        var func = await _db.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == _t.TenantId);
        if (func == null) return NotFound();

        func.Status      = dto.Status;
        func.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
