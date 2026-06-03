using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Models;

namespace VetClinica.API.Services.RH;

public class FechamentoService
{
    private readonly AppDbContext _db;

    public FechamentoService(AppDbContext db) => _db = db;

    public async Task<FechamentoMensal> GerarFechamentoAsync(
        Guid funcionarioId, DateOnly competencia, string? observacoes, Guid tenantId, Guid userId)
    {
        var func = await _db.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == funcionarioId && f.TenantId == tenantId)
            ?? throw new Exception("Funcionário não encontrado.");

        var compPrimeiro = new DateOnly(competencia.Year, competencia.Month, 1);

        var totalComissoes = await _db.Comissoes
            .Where(c => c.FuncionarioId == funcionarioId
                     && c.TenantId == tenantId
                     && c.Competencia == compPrimeiro
                     && c.Status == "pendente")
            .SumAsync(c => c.ValorComissao);

        var existing = await _db.FechamentosMensais
            .FirstOrDefaultAsync(f => f.TenantId == tenantId
                                   && f.FuncionarioId == funcionarioId
                                   && f.Competencia == compPrimeiro);

        if (existing != null)
        {
            existing.Salario = func.Salario;
            existing.TotalComissoes = totalComissoes;
            existing.TotalPagar = func.Salario + totalComissoes;
            existing.Status = "fechado";
            existing.Observacoes = observacoes;
            existing.FechadoEm = DateTime.UtcNow;
            existing.FechadoPor = userId;
        }
        else
        {
            existing = new FechamentoMensal
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FuncionarioId = funcionarioId,
                Competencia = compPrimeiro,
                Salario = func.Salario,
                TotalComissoes = totalComissoes,
                TotalPagar = func.Salario + totalComissoes,
                Status = "fechado",
                Observacoes = observacoes,
                FechadoEm = DateTime.UtcNow,
                FechadoPor = userId,
                CriadoEm = DateTime.UtcNow
            };
            _db.FechamentosMensais.Add(existing);
        }

        var comissoes = await _db.Comissoes
            .Where(c => c.FuncionarioId == funcionarioId
                     && c.TenantId == tenantId
                     && c.Competencia == compPrimeiro
                     && c.Status == "pendente")
            .ToListAsync();

        foreach (var c in comissoes)
        {
            c.Status = "pago";
            c.FechamentoId = existing.Id;
        }

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<List<FechamentoMensal>> GetByCompetenciaAsync(DateOnly competencia, Guid tenantId)
        => await _db.FechamentosMensais
            .Include(f => f.Funcionario)
            .Where(f => f.TenantId == tenantId && f.Competencia == competencia)
            .OrderBy(f => f.Funcionario!.Nome)
            .ToListAsync();

    public async Task MarcarComoPagoAsync(Guid id, Guid tenantId)
    {
        var f = await _db.FechamentosMensais
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId);
        if (f == null) return;
        f.Status = "pago";
        await _db.SaveChangesAsync();
    }
}
