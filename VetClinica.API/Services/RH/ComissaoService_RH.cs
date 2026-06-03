using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services.RH;

public class ComissaoService
{
    private readonly AppDbContext _db;

    public ComissaoService(AppDbContext db) => _db = db;

    /// <summary>
    /// Chamado automaticamente ao entregar uma OS.
    /// Verifica parâmetros da clínica e gera comissão se ativo.
    /// </summary>
    public async Task GerarComissaoOSAsync(OrdemServico os, Guid tenantId, AppDbContext? db = null)
    {
        var ctx = db ?? _db;

        // Sem funcionário vinculado, sem comissão
        if (os.FuncionarioId == null) return;

        // Verifica se comissão por OS está ativa para esta clínica
        var param = await ctx.ParametrosSistema
            .FirstOrDefaultAsync(p => p.TenantId == tenantId);

        if (param != null && !param.ComissaoOsAtivo) return;

        // Evita duplicidade
        var jaExiste = await ctx.Comissoes
            .AnyAsync(c => c.Origem == "os" && c.OrigemId == os.Id);
        if (jaExiste) return;

        var funcionario = await ctx.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == os.FuncionarioId && f.TenantId == tenantId);
        if (funcionario == null) return;

        var valorBase = os.ValorTotal ?? 0;
        var valorComissao = valorBase * (funcionario.PercentualComissao / 100);

        if (valorComissao <= 0) return;

        var competencia = DateOnly.FromDateTime(DateTime.Today);
        competencia = new DateOnly(competencia.Year, competencia.Month, 1);

        ctx.Comissoes.Add(new Comissao
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FuncionarioId = funcionario.Id,
            Origem = "os",
            OrigemId = os.Id,
            ValorBase = valorBase,
            Percentual = funcionario.PercentualComissao,
            ValorComissao = valorComissao,
            Status = "pendente",
            Competencia = competencia,
            CriadoEm = DateTime.UtcNow
        });

        // SaveChanges é responsabilidade do Controller (já chama SaveChangesAsync)
    }
}
