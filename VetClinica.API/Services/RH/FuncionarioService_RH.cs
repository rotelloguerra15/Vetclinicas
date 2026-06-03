using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Models;

namespace VetClinica.API.Services.RH;

public class FuncionarioService
{
    private readonly AppDbContext _db;

    public FuncionarioService(AppDbContext db) => _db = db;

    public async Task<List<Funcionario>> GetAllAsync(Guid tenantId)
        => await _db.Funcionarios
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.Nome)
            .ToListAsync();

    public async Task<Funcionario?> GetByIdAsync(Guid id, Guid tenantId)
        => await _db.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId);

    public async Task<Funcionario> CreateAsync(Funcionario func)
    {
        func.Id = Guid.NewGuid();
        func.CriadoEm = DateTime.UtcNow;
        func.AtualizadoEm = DateTime.UtcNow;
        _db.Funcionarios.Add(func);
        await _db.SaveChangesAsync();
        return func;
    }

    public async Task<Funcionario?> UpdateAsync(Guid id, Funcionario dados, Guid tenantId)
    {
        var func = await GetByIdAsync(id, tenantId);
        if (func == null) return null;

        func.Nome = dados.Nome;
        func.Cpf = dados.Cpf;
        func.Rg = dados.Rg;
        func.DataNascimento = dados.DataNascimento;
        func.Logradouro = dados.Logradouro;
        func.Numero = dados.Numero;
        func.Complemento = dados.Complemento;
        func.Bairro = dados.Bairro;
        func.Cidade = dados.Cidade;
        func.Estado = dados.Estado;
        func.Cep = dados.Cep;
        func.Telefone = dados.Telefone;
        func.Email = dados.Email;
        func.Cargo = dados.Cargo;
        func.DataAdmissao = dados.DataAdmissao;
        func.Salario = dados.Salario;
        func.PercentualComissao = dados.PercentualComissao;
        func.Status = dados.Status;
        func.AtualizadoEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return func;
    }

    public async Task UpdateStatusAsync(Guid id, string status, Guid tenantId)
    {
        var func = await GetByIdAsync(id, tenantId);
        if (func == null) return;
        func.Status = status;
        func.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
