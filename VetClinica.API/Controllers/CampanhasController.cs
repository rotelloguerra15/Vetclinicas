using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Models;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/campanhas")]
public class CampanhasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public CampanhasController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    private bool PodeAprovar => _t.Papel == "owner" || _t.Papel == "admin";

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var items = await _db.Campanhas
            .Where(c => c.TenantId == _t.TenantId)
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => new CampanhaDto(c.Id, c.Titulo, c.Mensagem, c.ImagemUrl,
                c.Publico, c.Status, c.TotalDestinatarios, c.TotalEnviados,
                c.MotivoReprovacao, c.CriadoEm))
            .ToListAsync();
        return Ok(items);
    }

    // Monta a query do público-alvo (reutilizada na prévia e na aprovação)
    private IQueryable<Tutor> PublicoQuery(string publico)
    {
        var q = _db.Tutores.Where(t => t.TenantId == _t.TenantId && t.Ativo
                                    && t.AceitaPromocoes && t.Telefone != null);

        if (publico == "inativos_30d")
        {
            // tutores sem nenhuma OS entregue nos últimos 30 dias
            var limite = DateTime.UtcNow.AddDays(-30);
            var ativosRecentes = _db.OrdensServico
                .Where(o => o.TenantId == _t.TenantId && o.EntregueEm != null && o.EntregueEm >= limite)
                .Join(_db.Pets, o => o.PetId, p => p.Id, (o, p) => p.TutorId);
            q = q.Where(t => !ativosRecentes.Contains(t.Id));
        }
        else if (publico == "aniversariantes_mes")
        {
            var mes = (short)DateTime.Today.Month;
            q = q.Where(t => t.AniversarioMes == mes);
        }
        return q;
    }

    // Prévia: quantos tutores receberiam
    [HttpGet("previa")]
    public async Task<IActionResult> Previa([FromQuery] string publico = "todos")
    {
        var total = await PublicoQuery(publico).CountAsync();
        return Ok(new PreviaPublico(total));
    }

    // Cria a campanha — nasce AGUARDANDO APROVAÇÃO (qualquer papel pode criar)
    [HttpPost]
    public async Task<IActionResult> Criar(CampanhaCreate dto)
    {
        var c = new Campanha
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            Titulo = dto.Titulo,
            Mensagem = dto.Mensagem,
            ImagemUrl = dto.ImagemUrl,
            Publico = dto.Publico,
            Status = "aguardando_aprovacao",
            CriadoPor = _t.UserId,
            TotalDestinatarios = await PublicoQuery(dto.Publico).CountAsync(),
            CriadoEm = DateTime.UtcNow
        };
        _db.Campanhas.Add(c);
        await _db.SaveChangesAsync();
        return Ok(new { c.Id, c.TotalDestinatarios });
    }

    // APROVAR — só dono/admin. Expande o público para a fila de envio.
    [HttpPut("{id}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id)
    {
        if (!PodeAprovar)
            return StatusCode(403, new { erro = "Apenas o dono ou administrador da clínica pode aprovar campanhas" });

        var c = await _db.Campanhas.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (c == null) return NotFound();
        if (c.Status != "aguardando_aprovacao")
            return BadRequest(new { erro = "Campanha não está aguardando aprovação" });

        var destinatarios = await PublicoQuery(c.Publico)
            .Select(t => new { t.Id, t.Telefone, t.Nome })
            .ToListAsync();

        var payloadBase = JsonSerializer.Serialize(new
        {
            mensagem = c.Mensagem,
            fotos = c.ImagemUrl != null ? new[] { c.ImagemUrl } : Array.Empty<string>()
        });

        foreach (var d in destinatarios)
        {
            _db.NotificacoesFila.Add(new NotificacaoFila
            {
                Id = Guid.NewGuid(),
                TenantId = _t.TenantId,
                TutorId = d.Id,
                Canal = c.Canal,
                Tipo = "promocao",
                Status = "pendente",
                Destinatario = d.Telefone,
                Payload = payloadBase,
                AgendadoPara = DateTime.UtcNow,
                CriadoEm = DateTime.UtcNow
            });
        }

        c.Status = "enviada";        // enfileirada; o worker faz o envio em lotes
        c.AprovadoPor = _t.UserId;
        c.AprovadoEm = DateTime.UtcNow;
        c.EnviadaEm = DateTime.UtcNow;
        c.TotalDestinatarios = destinatarios.Count;
        c.TotalEnviados = destinatarios.Count;
        await _db.SaveChangesAsync();

        return Ok(new { enfileirados = destinatarios.Count });
    }

    [HttpPut("{id}/reprovar")]
    public async Task<IActionResult> Reprovar(Guid id, ReprovarRequest dto)
    {
        if (!PodeAprovar)
            return StatusCode(403, new { erro = "Apenas o dono ou administrador pode reprovar" });

        var c = await _db.Campanhas.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _t.TenantId);
        if (c == null) return NotFound();
        c.Status = "reprovada";
        c.MotivoReprovacao = dto.Motivo;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
