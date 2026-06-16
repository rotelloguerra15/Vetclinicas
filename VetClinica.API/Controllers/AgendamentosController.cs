using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Models;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/agendamentos")]
public class AgendamentosController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext _t;
    private readonly AgendaService _agenda;
    public AgendamentosController(TenantDbContextFactory factory, TenantContext t, AgendaService agenda)
    { _db = factory.Create(); _t = t; _agenda = agenda; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateTime? de, [FromQuery] DateTime? ate)
    {
        var inicio = de ?? DateTime.Today;
        var fim = ate ?? inicio.AddDays(7);

        var ags = await _db.Agendamentos
            .Include(a => a.Pet)
            .Include(a => a.Servico)
            .Where(a => a.TenantId == _t.TenantId && a.DataHora >= inicio && a.DataHora < fim)
            .OrderBy(a => a.DataHora)
            .Select(a => new {
                a.Id,
                PetId       = a.PetId,
                PetNome     = a.Pet!.Nome,
                a.Tipo,
                a.ServicoId,
                ServicoNome    = a.Servico != null ? a.Servico.Nome : null,
                ServicoDuracao = a.Servico != null ? a.Servico.DuracaoMin : (int?)null,
                Icone          = a.Servico != null ? a.Servico.Icone : null,
                a.DataHora,
                a.DuracaoMin,
                a.Status,
                a.Origem,
                a.Obs
            })
            .ToListAsync();
        return Ok(ags);
    }

    [HttpGet("slots")]
    public async Task<IActionResult> Slots([FromQuery] int dias = 14)
    {
        var slots = await _agenda.GetSlotsLivres(_t.TenantId, DateTime.Today, dias);
        return Ok(slots);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(AgendamentoCreate dto)
    {
        var ag = new Agendamento
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            PetId = dto.PetId,
            UserId = dto.UserId,
            Tipo = dto.Tipo,
            DataHora = dto.DataHora,
            DuracaoMin = dto.DuracaoMin,
            Status = "confirmado",
            Origem = "interno",
            Obs = dto.Obs,
            ServicoId = dto.ServicoId,
            CriadoEm = DateTime.UtcNow
        };
        _db.Agendamentos.Add(ag);
        await _db.SaveChangesAsync();
        return Ok(ag);
    }

    [HttpPut("{id}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id)
    {
        var ag = await _db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _t.TenantId);
        if (ag == null) return NotFound();
        ag.Status = "confirmado";
        ag.ConfirmadoEm = DateTime.UtcNow;
        ag.ConfirmadoPor = _t.UserId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] StatusUpdate dto)
    {
        var ag = await _db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _t.TenantId);
        if (ag == null) return NotFound();
        ag.Status = dto.Status;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/agendamentos/{id}/iniciar-atendimento
    // Cria OS vinculada ao agendamento, muda status para em_atendimento
    // Retorna petId para o frontend navegar direto para PetDetalhe
    [HttpPost("{id}/iniciar-atendimento")]
    public async Task<IActionResult> IniciarAtendimento(Guid id)
    {
        var ag = await _db.Agendamentos
            .Include(a => a.Pet)
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _t.TenantId);

        if (ag == null) return NotFound();
        if (ag.Status == "cancelado")
            return BadRequest(new { erro = "Agendamento cancelado não pode ser iniciado." });

        // Verifica se já tem OS aberta para este agendamento
        var osExistente = await _db.OrdensServico
            .FirstOrDefaultAsync(o => o.AgendamentoId == id
                                   && o.TenantId == _t.TenantId
                                   && o.Status != "cancelada");

        Guid osId;
        if (osExistente != null)
        {
            osId = osExistente.Id;
        }
        else
        {
            // Busca funcionário vinculado ao usuário logado
            var funcionario = await _db.Funcionarios
                .FirstOrDefaultAsync(f => f.TenantId == _t.TenantId
                                       && f.UsuarioId == _t.UserId
                                       && f.Status == "trabalhando");

            // Calcula valor total com o serviço do agendamento (se houver)
            decimal valorTotal = ag.Servico?.PrecoBase ?? 0;

            var novaOs = new VetClinica.API.Models.OrdemServico
            {
                Id            = Guid.NewGuid(),
                TenantId      = _t.TenantId,
                AgendamentoId = id,
                PetId         = ag.PetId,
                UserId        = _t.UserId,
                FuncionarioId = funcionario?.Id,
                Status        = "em_andamento",
                ValorTotal    = valorTotal,
                Inicio        = DateTime.UtcNow,
                CriadoEm      = DateTime.UtcNow
            };
            _db.OrdensServico.Add(novaOs);

            // Vincula o serviço do agendamento à OS
            if (ag.ServicoId.HasValue && ag.Servico != null)
            {
                _db.OsServicos.Add(new VetClinica.API.Models.OsServico
                {
                    Id           = Guid.NewGuid(),
                    OsId         = novaOs.Id,
                    ServicoId    = ag.ServicoId.Value,
                    PrecoCobrado = ag.Servico.PrecoBase,
                    Obs          = null
                });
            }

            osId = novaOs.Id;
        }

        // Atualiza status do agendamento
        ag.Status = "em_atendimento";
        await _db.SaveChangesAsync();

        return Ok(new {
            osId,
            petId       = ag.PetId,
            petNome     = ag.Pet?.Nome,
            servicoNome = ag.Servico?.Nome
        });
    }
}
