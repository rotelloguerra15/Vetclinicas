using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Models;
using VetClinica.API.Services.RH;

namespace VetClinica.API.Controllers;

// ================================================================
//  COMISSÕES
// ================================================================
[ApiController]
[Authorize]
[Route("api/rh/comissoes")]
public class ComissoesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;

    public ComissoesController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? funcionarioId,
        [FromQuery] DateOnly? competencia,
        [FromQuery] string? status)
    {
        var query = _db.Comissoes
            .Include(c => c.Funcionario)
            .Where(c => c.TenantId == _t.TenantId)
            .AsQueryable();

        if (funcionarioId.HasValue)
            query = query.Where(c => c.FuncionarioId == funcionarioId.Value);

        if (competencia.HasValue)
            query = query.Where(c => c.Competencia == new DateOnly(competencia.Value.Year, competencia.Value.Month, 1));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        var lista = await query
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => new ComissaoDto(
                c.Id, c.FuncionarioId,
                c.Funcionario != null ? c.Funcionario.Nome : "",
                c.Origem, c.ValorBase, c.Percentual,
                c.ValorComissao, c.Status, c.Competencia, c.CriadoEm))
            .ToListAsync();

        return Ok(lista);
    }
}

// ================================================================
//  FECHAMENTO MENSAL
// ================================================================
[ApiController]
[Authorize]
[Route("api/rh/fechamento")]
public class FechamentoController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    private readonly FechamentoService _service;

    public FechamentoController(AppDbContext db, TenantContext t, FechamentoService service)
    { _db = db; _t = t; _service = service; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? competencia)
    {
        var comp = competencia.HasValue
            ? new DateOnly(competencia.Value.Year, competencia.Value.Month, 1)
            : new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

        var lista = await _db.FechamentosMensais
            .Include(f => f.Funcionario)
            .Where(f => f.TenantId == _t.TenantId && f.Competencia == comp)
            .OrderBy(f => f.Funcionario!.Nome)
            .Select(f => new FechamentoDto(
                f.Id, f.FuncionarioId,
                f.Funcionario != null ? f.Funcionario.Nome : "",
                f.Competencia, f.Salario, f.TotalComissoes,
                f.TotalPagar, f.Status, f.Observacoes, f.FechadoEm))
            .ToListAsync();

        return Ok(lista);
    }

    [HttpPost]
    public async Task<IActionResult> Fechar(FechamentoCreate dto)
    {
        var fechamento = await _service.GerarFechamentoAsync(
            dto.FuncionarioId, dto.Competencia, dto.Observacoes,
            _t.TenantId, _t.UserId);

        return Ok(new FechamentoDto(
            fechamento.Id, fechamento.FuncionarioId,
            fechamento.Funcionario?.Nome ?? "",
            fechamento.Competencia, fechamento.Salario,
            fechamento.TotalComissoes, fechamento.TotalPagar,
            fechamento.Status, fechamento.Observacoes, fechamento.FechadoEm));
    }

    [HttpPatch("{id:guid}/pago")]
    public async Task<IActionResult> MarcarPago(Guid id)
    {
        await _service.MarcarComoPagoAsync(id, _t.TenantId);
        return NoContent();
    }
}

// ================================================================
//  PARÂMETROS DO SISTEMA
// ================================================================
[ApiController]
[Authorize]
[Route("api/rh/parametros")]
public class ParametrosSistemaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;

    public ParametrosSistemaController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet]
    public async Task<IActionResult> Obter()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(p => p.TenantId == _t.TenantId);
        return Ok(new ParametrosSistemaDto(p?.ComissaoOsAtivo ?? true, p?.ComissaoPdvAtivo ?? false));
    }

    [HttpPut]
    public async Task<IActionResult> Salvar(ParametrosSistemaDto dto)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(p => p.TenantId == _t.TenantId);

        if (p == null)
        {
            p = new ParametrosSistema
            {
                Id = Guid.NewGuid(),
                TenantId = _t.TenantId,
                CriadoEm = DateTime.UtcNow
            };
            _db.ParametrosSistema.Add(p);
        }

        p.ComissaoOsAtivo = dto.ComissaoOsAtivo;
        p.ComissaoPdvAtivo = dto.ComissaoPdvAtivo;
        p.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

// ================================================================
//  RELATÓRIOS RH
// ================================================================
[ApiController]
[Authorize]
[Route("api/rh/relatorios")]
public class RHRelatoriosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;

    public RHRelatoriosController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    [HttpGet("mensal")]
    public async Task<IActionResult> Mensal([FromQuery] DateOnly? competencia)
    {
        var comp = competencia.HasValue
            ? new DateOnly(competencia.Value.Year, competencia.Value.Month, 1)
            : new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

        var funcionarios = await _db.Funcionarios
            .Where(f => f.TenantId == _t.TenantId && f.Status != "demitido")
            .ToListAsync();

        var comissoes = await _db.Comissoes
            .Where(c => c.TenantId == _t.TenantId && c.Competencia == comp)
            .ToListAsync();

        var ossDoMes = await _db.OrdensServico
            .Where(o => o.TenantId == _t.TenantId && o.Status == "entregue"
                     && o.FuncionarioId != null
                     && new DateOnly(o.CriadoEm.Year, o.CriadoEm.Month, 1) == comp)
            .ToListAsync();

        var resultado = funcionarios.Select(f => new RelatorioRHItem(
            f.Nome, f.Cargo, f.Salario,
            comissoes.Where(c => c.FuncionarioId == f.Id).Sum(c => c.ValorComissao),
            f.Salario + comissoes.Where(c => c.FuncionarioId == f.Id).Sum(c => c.ValorComissao),
            ossDoMes.Count(o => o.FuncionarioId == f.Id),
            f.Status)).ToList();

        return Ok(resultado);
    }

    [HttpGet("custo-mensal")]
    public async Task<IActionResult> CustoMensal()
    {
        var fechamentos = await _db.FechamentosMensais
            .Where(f => f.TenantId == _t.TenantId
                     && f.Competencia >= new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5))
            .ToListAsync();

        var resultado = fechamentos
            .GroupBy(f => f.Competencia)
            .OrderBy(g => g.Key)
            .Select(g => new CustoMensalItem(g.Key.ToString("MM/yyyy"), g.Sum(f => f.TotalPagar)))
            .ToList();

        return Ok(resultado);
    }

    [HttpGet("produtividade")]
    public async Task<IActionResult> Produtividade([FromQuery] DateOnly? competencia)
    {
        var comp = competencia.HasValue
            ? new DateOnly(competencia.Value.Year, competencia.Value.Month, 1)
            : new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

        var funcionarios = await _db.Funcionarios
            .Where(f => f.TenantId == _t.TenantId && f.Status == "trabalhando")
            .ToListAsync();

        var oss = await _db.OrdensServico
            .Where(o => o.TenantId == _t.TenantId && o.Status == "entregue"
                     && o.FuncionarioId != null
                     && new DateOnly(o.CriadoEm.Year, o.CriadoEm.Month, 1) == comp)
            .ToListAsync();

        var comissoes = await _db.Comissoes
            .Where(c => c.TenantId == _t.TenantId && c.Competencia == comp)
            .ToListAsync();

        var resultado = funcionarios.Select(f => new ProdutividadeItem(
            f.Nome,
            oss.Count(o => o.FuncionarioId == f.Id),
            oss.Where(o => o.FuncionarioId == f.Id).Sum(o => o.ValorTotal ?? 0),
            comissoes.Where(c => c.FuncionarioId == f.Id).Sum(c => c.ValorComissao)
        )).OrderByDescending(p => p.TotalOs).ToList();

        return Ok(resultado);
    }
}
