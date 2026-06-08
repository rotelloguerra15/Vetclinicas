using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;

namespace VetClinica.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/gestao-vista")]
public class GestaoVistaController : ControllerBase
{
    private readonly AppDbContext _db;

    public GestaoVistaController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/gestao-vista/{tenantId}
    // Retorna OS abertas + agendamentos do dia para a TV da recepção (sem auth)
    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId)
    {
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);

        // Branding da clínica
        var tenant = await _db.Tenants
            .Where(t => t.Id == tenantId && t.Ativo)
            .Select(t => new { t.Nome, t.Tagline, t.LogoUrl })
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound(new { erro = "Clínica não encontrada" });

        // OS abertas (exceto canceladas e entregues)
        var ordens = await (
            from o in _db.OrdensServico
            join p in _db.Pets on o.PetId equals p.Id
            join tu in _db.Tutores on p.TutorId equals tu.Id into gt
            from tu in gt.DefaultIfEmpty()
            where o.TenantId == tenantId
               && o.Status != "cancelado"
               && o.Status != "entregue"
            orderby o.CriadoEm
            select new
            {
                o.Id,
                PetNome = p.Nome,
                PetEspecie = p.Especie,
                PetRaca = p.Raca,
                TutorNome = tu != null ? tu.Nome : null,
                o.Status,
                o.Inicio,
                o.Fim,
                o.CriadoEm
            }
        ).ToListAsync();

        // Busca servicos de cada OS
        var osIds = ordens.Select(o => o.Id).ToList();
        var servicos = await (
            from os_s in _db.OsServicos
            join s in _db.Servicos on os_s.ServicoId equals s.Id
            where osIds.Contains(os_s.OsId)
            select new { os_s.OsId, ServicoNome = s.Nome }
        ).ToListAsync();

        var servicosPorOs = servicos
            .GroupBy(s => s.OsId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.ServicoNome).ToList());

        var ordensResult = ordens.Select(o => new
        {
            o.Id,
            o.PetNome,
            o.PetEspecie,
            o.PetRaca,
            o.TutorNome,
            o.Status,
            o.Inicio,
            o.Fim,
            o.CriadoEm,
            Servicos = servicosPorOs.TryGetValue(o.Id, out var s) ? s : new List<string>()
        }).ToList();

        // Agendamentos de hoje
        var agendamentos = await _db.Agendamentos
            .Where(a => a.TenantId == tenantId
                     && a.DataHora >= hoje
                     && a.DataHora < amanha
                     && a.Status != "cancelado")
            .OrderBy(a => a.DataHora)
            .Select(a => new
            {
                a.Id,
                PetNome = _db.Pets.Where(p => p.Id == a.PetId).Select(p => p.Nome).FirstOrDefault(),
                a.Tipo,
                a.DataHora,
                a.DuracaoMin,
                a.Status
            })
            .ToListAsync();

        // Contadores
        var totalHoje = await _db.Agendamentos
            .CountAsync(a => a.TenantId == tenantId
                          && a.DataHora >= hoje
                          && a.DataHora < amanha
                          && a.Status != "cancelado");

        var finalizadosHoje = await _db.OrdensServico
            .CountAsync(o => o.TenantId == tenantId
                          && o.Status == "entregue"
                          && o.EntregueEm >= hoje
                          && o.EntregueEm < amanha);

        return Ok(new
        {
            clinica = new
            {
                tenant.Nome,
                tenant.Tagline,
                tenant.LogoUrl
            },
            contadores = new
            {
                totalHoje,
                finalizadosHoje,
                aguardando = ordensResult.Count(o => o.Status == "aguardando"),
                emAndamento = ordensResult.Count(o => o.Status == "em_andamento"),
                prontos = ordensResult.Count(o => o.Status == "pronto")
            },
            ordens = ordensResult,
            agendamentos
        });
    }
}
