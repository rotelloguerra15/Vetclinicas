using System.Text.Json;
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
[Route("api/ordens-servico")]
public class OrdensServicoController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    private readonly ComissaoService _comissao;

    public OrdensServicoController(AppDbContext db, TenantContext t, ComissaoService comissao)
    {
        _db = db;
        _t = t;
        _comissao = comissao;
    }

    [HttpGet("abertas")]
    public async Task<IActionResult> Abertas()
    {
        var oss = await (
            from o in _db.OrdensServico
            join p in _db.Pets on o.PetId equals p.Id
            join t in _db.Tutores on p.TutorId equals t.Id into gt
            from t in gt.DefaultIfEmpty()
            where o.TenantId == _t.TenantId && o.Status != "cancelado"
            orderby o.CriadoEm
            select new OsDto(o.Id, o.PetId, p.Nome, t != null ? (Guid?)t.Id : null,
                t != null ? t.Nome : null, o.Status, o.ValorTotal, o.Inicio, o.Fim, o.FotosUrls)
        ).ToListAsync();
        return Ok(oss);
    }

    [HttpGet("{id}/servicos")]
    public async Task<IActionResult> Servicos(Guid id)
    {
        var os = await _db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _t.TenantId);
        if (os == null) return NotFound();

        var itens = await (
            from os_s in _db.OsServicos
            join s in _db.Servicos on os_s.ServicoId equals s.Id
            where os_s.OsId == id
            select new OsServicoDto(os_s.Id, os_s.ServicoId, s.Nome, os_s.PrecoCobrado, os_s.Obs)
        ).ToListAsync();

        return Ok(itens);
    }

    [HttpPost]
    public async Task<IActionResult> Abrir(OsCreate dto)
    {
        var valorTotal = dto.Servicos.Sum(s => s.PrecoCobrado);

        var os = new OrdemServico
        {
            Id = Guid.NewGuid(),
            TenantId = _t.TenantId,
            AgendamentoId = dto.AgendamentoId,
            PetId = dto.PetId,
            UserId = _t.UserId,
            FuncionarioId = dto.FuncionarioId,
            Status = "aguardando",
            ValorTotal = valorTotal,
            CriadoEm = DateTime.UtcNow
        };
        _db.OrdensServico.Add(os);

        foreach (var s in dto.Servicos)
        {
            _db.OsServicos.Add(new OsServico
            {
                Id = Guid.NewGuid(),
                OsId = os.Id,
                ServicoId = s.ServicoId,
                PrecoCobrado = s.PrecoCobrado,
                Obs = s.Obs
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { os.Id });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, OsStatusUpdate dto)
    {
        var os = await _db.OrdensServico.Include(o => o.Pet)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _t.TenantId);
        if (os == null) return NotFound();

        var statusAnterior = os.Status;
        os.Status = dto.Status;

        if (dto.Status == "em_andamento" && os.Inicio == null) os.Inicio = DateTime.UtcNow;
        if (dto.Status == "pronto" && os.Fim == null) os.Fim = DateTime.UtcNow;
        if (dto.Status == "entregue") os.EntregueEm = DateTime.UtcNow;

        // ── Notificação "pet pronto" ──────────────────────────────────
        if (dto.Status == "pronto" && statusAnterior != "pronto")
        {
            var tutor = await _db.Tutores
                .Where(tu => tu.Id == _db.Pets.Where(p => p.Id == os.PetId)
                    .Select(p => p.TutorId).First())
                .FirstOrDefaultAsync();

            if (tutor?.Telefone != null)
            {
                var payload = JsonSerializer.Serialize(new
                {
                    pet_nome = os.Pet!.Nome,
                    fotos = os.FotosUrls ?? Array.Empty<string>()
                });

                _db.NotificacoesFila.Add(new NotificacaoFila
                {
                    Id = Guid.NewGuid(),
                    TenantId = _t.TenantId,
                    OsId = os.Id,
                    PetId = os.PetId,
                    TutorId = tutor.Id,
                    Canal = "whatsapp",
                    Tipo = "pet_pronto",
                    Status = "pendente",
                    Destinatario = tutor.Telefone,
                    Payload = payload,
                    AgendadoPara = DateTime.UtcNow,
                    CriadoEm = DateTime.UtcNow
                });
            }
        }

        // ── Vínculo financeiro: OS entregue → Conta a Receber ─────────
        if (dto.Status == "entregue" && statusAnterior != "entregue")
        {
            // Gera comissão automaticamente se funcionário vinculado
            await _comissao.GerarComissaoOSAsync(os, _t.TenantId, _db);

            // Só gera se a OS tem valor e ainda não tem conta vinculada
            var jaTemConta = await _db.Contas
                .AnyAsync(c => c.OsId == os.Id && c.TenantId == _t.TenantId);

            if (!jaTemConta && os.ValorTotal.HasValue && os.ValorTotal > 0)
            {
                var pet = os.Pet ?? await _db.Pets.FirstOrDefaultAsync(p => p.Id == os.PetId);
                var nomeOs = $"OS — {pet?.Nome ?? "Pet"} ({os.Id.ToString()[..8].ToUpper()})";

                // Tenta encontrar categoria "Banho e Tosa" ou "Consultas" como padrão
                var categoriaId = await _db.CategoriasFinanceiras
                    .Where(c => c.TenantId == _t.TenantId && c.Tipo == "receita" && c.Ativo)
                    .OrderBy(c => c.Nome)
                    .Select(c => (Guid?)c.Id)
                    .FirstOrDefaultAsync();

                _db.Contas.Add(new Conta
                {
                    Id = Guid.NewGuid(),
                    TenantId = _t.TenantId,
                    OsId = os.Id,
                    Tipo = "receita",
                    Descricao = nomeOs,
                    Valor = os.ValorTotal.Value,
                    DataCompetencia = DateOnly.FromDateTime(DateTime.Today),
                    DataVencimento = DateOnly.FromDateTime(DateTime.Today),
                    Status = "aberta",
                    CategoriaId = categoriaId,
                    CriadoPor = _t.UserId,
                    CriadoEm = DateTime.UtcNow,
                    AtualizadoEm = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/fotos")]
    public async Task<IActionResult> AdicionarFotos(Guid id, [FromBody] string[] urls)
    {
        var os = await _db.OrdensServico.FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _t.TenantId);
        if (os == null) return NotFound();
        var atuais = os.FotosUrls?.ToList() ?? new List<string>();
        atuais.AddRange(urls);
        os.FotosUrls = atuais.ToArray();
        await _db.SaveChangesAsync();
        return Ok(new { os.FotosUrls });
    }
}
