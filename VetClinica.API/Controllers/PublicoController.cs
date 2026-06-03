using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Models;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/publico")]
public class PublicoController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AgendaService _agenda;
    public PublicoController(AppDbContext db, AgendaService agenda) { _db = db; _agenda = agenda; }

    // Tutor abre o link recebido por WhatsApp e vê horários livres
    [HttpGet("agendar/{token}")]
    public async Task<IActionResult> GetSlots(string token)
    {
        var link = await _db.LinksAgendamento
            .FirstOrDefaultAsync(l => l.Token == token
                && l.ExpiraEm > DateTime.UtcNow && l.UsadoEm == null);
        if (link == null) return NotFound(new { erro = "Link expirado ou inválido" });

        string? petNome = null;
        if (link.PetId != null)
            petNome = await _db.Pets.Where(p => p.Id == link.PetId).Select(p => p.Nome).FirstOrDefaultAsync();

        var slots = await _agenda.GetSlotsLivres(link.TenantId, DateTime.Today, 14);
        return Ok(new SlotsResponse(link.PetId ?? Guid.Empty, link.TutorId, petNome, slots));
    }

    // Tutor escolhe um horário → cria agendamento PENDENTE (clínica confirma depois)
    [HttpPost("agendar/{token}")]
    public async Task<IActionResult> Agendar(string token, AgendarPublicoRequest req)
    {
        var link = await _db.LinksAgendamento
            .FirstOrDefaultAsync(l => l.Token == token
                && l.ExpiraEm > DateTime.UtcNow && l.UsadoEm == null);
        if (link == null) return NotFound(new { erro = "Link expirado ou inválido" });
        if (link.PetId == null) return BadRequest(new { erro = "Link sem pet associado" });

        var ag = new Agendamento
        {
            Id = Guid.NewGuid(),
            TenantId = link.TenantId,
            PetId = link.PetId.Value,
            Tipo = req.Tipo,
            DataHora = req.DataHora,
            DuracaoMin = 60,
            Status = "pendente",                 // aguarda confirmação da clínica
            Origem = "tutor_self_service",
            CriadoEm = DateTime.UtcNow
        };
        _db.Agendamentos.Add(ag);

        link.UsadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { mensagem = "Pedido de agendamento recebido! A clínica vai confirmar em breve.", ag.Id });
    }
}
