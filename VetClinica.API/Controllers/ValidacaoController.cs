using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;

namespace VetClinica.API.Controllers;

[ApiController]
[Route("api/validar")]
public class ValidacaoController : ControllerBase
{
    private readonly TenantDbContext _db;
    public ValidacaoController(TenantDbContextFactory factory) { _db = factory.Create(); }

    [HttpGet("{codigo}")]
    public async Task<IActionResult> Validar(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(new { valido = false, erro = "Codigo invalido." });

        var item = await _db.ProntuarioItens
            .Where(p => p.Tipo == "receita"
                     && p.Titulo != null
                     && p.Titulo.StartsWith($"[{codigo}]"))
            .FirstOrDefaultAsync();

        if (item == null)
            return Ok(new { valido = false, mensagem = "Documento nao encontrado ou codigo invalido." });

        // Busca pet e tutor
        var pet = await _db.Pets
            .Include(p => p.Tutor)
            .FirstOrDefaultAsync(p => p.Id == item.PetId);

        // Busca tenant
        var tenantId = pet?.TenantId ?? Guid.Empty;
        var tenant   = tenantId != Guid.Empty
            ? await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId)
            : null;

        // Veterinário via UserId
        string? vetNome = null, vetCrmv = null;
        if (item.UserId.HasValue)
        {
            var vet = await _db.Funcionarios
                .FirstOrDefaultAsync(f => f.UsuarioId == item.UserId.Value);
            vetNome = vet?.Nome;
            vetCrmv = vet?.Crmv;
        }

        var linhasReceita = (item.Receituario ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !l.StartsWith("---") && !l.StartsWith("Receituario emitido"))
            .ToList();

        return Ok(new {
            valido      = true,
            codigo,
            emitidoEm   = item.Data.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            clinica = new {
                nome     = tenant?.Nome ?? "—",
                telefone = tenant?.Telefone
            },
            veterinario = new {
                nome = vetNome ?? "—",
                crmv = vetCrmv
            },
            paciente = new {
                nome    = pet?.Nome ?? "—",
                especie = pet?.Especie,
                raca    = pet?.Raca
            },
            tutor = new {
                nome = pet?.Tutor?.Nome ?? "—"
            },
            motivo       = item.Motivo,
            medicamentos = linhasReceita,
            mensagem     = "Documento autentico. Emitido pelo sistema VetClinica."
        });
    }
}
