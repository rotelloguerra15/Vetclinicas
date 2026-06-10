using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;

namespace VetClinica.API.Controllers;

/// <summary>
/// Endpoint PÚBLICO (sem autenticação) para validar receituários
/// Qualquer pessoa (farmácia, fiscal, tutor) pode verificar a autenticidade
/// </summary>
[ApiController]
[Route("api/validar")]
public class ValidacaoController : ControllerBase
{
    private readonly AppDbContext _db;
    public ValidacaoController(AppDbContext db) { _db = db; }

    // GET /api/validar/{codigo}
    // ex: /api/validar/REC-2026-A1B2C3D4
    [HttpGet("{codigo}")]
    public async Task<IActionResult> Validar(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(new { valido = false, erro = "Codigo invalido." });

        // Busca o prontuário pelo código salvo no Titulo
        var item = await _db.ProntuarioItens
            .Include(p => p.Pet)
                .ThenInclude(p => p.Tutor)
            .Where(p => p.Tipo == "receita" && p.Titulo != null && p.Titulo.StartsWith($"[{codigo}]"))
            .FirstOrDefaultAsync();

        if (item == null)
            return Ok(new { valido = false, mensagem = "Documento não encontrado ou código inválido." });

        // Busca dados da clínica via tenant do pet
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == _db.Pets
                .Where(p => p.Id == item.PetId)
                .Select(p => p.TenantId)
                .FirstOrDefault());

        // Extrai veterinário a partir do UserId do prontuário
        string? vetNome = null, vetCrmv = null;
        if (item.UserId.HasValue)
        {
            var vet = await _db.Funcionarios
                .FirstOrDefaultAsync(f => f.UsuarioId == item.UserId.Value);
            vetNome = vet?.Nome;
            vetCrmv = vet?.Crmv;
        }

        // Monta os medicamentos a partir do campo receituario (texto)
        var linhasReceita = (item.Receituario ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return Ok(new {
            valido      = true,
            codigo,
            emitidoEm   = item.Data.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            clinica     = new {
                nome     = tenant?.Nome ?? "—",
                telefone = tenant?.Telefone,
            },
            veterinario = new {
                nome = vetNome ?? "—",
                crmv = vetCrmv
            },
            paciente = new {
                nome    = item.Pet?.Nome ?? "—",
                especie = item.Pet?.Especie,
                raca    = item.Pet?.Raca,
            },
            tutor = new {
                nome = item.Pet?.Tutor?.Nome ?? "—",
            },
            motivo       = item.Motivo,
            medicamentos = linhasReceita
                .Where(l => !l.StartsWith("---") && !l.StartsWith("Receituario emitido"))
                .ToList(),
            mensagem = "Documento autentico. Emitido pelo sistema VetClinica."
        });
    }
}
