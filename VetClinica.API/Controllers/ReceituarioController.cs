using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

public record ReceituarioRequest(
    Guid PetId,
    Guid? FuncionarioId,
    string? TipoReceita,
    string? ViaUso,
    string? TipoFarmacia,
    string? Motivo,
    List<MedicamentoItemRequest> Medicamentos,
    string? Observacoes,
    bool EnviarWhatsApp
);

public record MedicamentoItemRequest(
    string Nome,
    string? Apresentacao,
    string? Dosagem,
    string? Frequencia,
    string? Duracao,
    string? Via,
    string? Quantidade
);

public record ReimprimirRequest(Guid ProntuarioItemId, bool EnviarWhatsApp);

[ApiController]
[Authorize]
[Route("api/receituario")]
public class ReceituarioController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    private readonly ReceituarioPdfService _pdf;
    private readonly ZApiService _zapi;

    public ReceituarioController(AppDbContext db, TenantContext t, ReceituarioPdfService pdf, ZApiService zapi)
    { _db = db; _t = t; _pdf = pdf; _zapi = zapi; }

    [HttpGet("veterinarios")]
    public async Task<IActionResult> ListarVeterinarios()
    {
        var lista = await _db.Funcionarios
            .Where(f => f.TenantId == _t.TenantId
                     && f.Status == "trabalhando"
                     && f.AssinaReceituario)
            .OrderBy(f => f.Nome)
            .Select(f => new { f.Id, f.Nome, f.Cargo, f.Crmv, f.RegistroMapa, f.Telefone, f.Email })
            .ToListAsync();
        return Ok(lista);
    }

    [HttpPost]
    public async Task<IActionResult> GerarReceituario(ReceituarioRequest req)
    {
        var pet = await _db.Pets.Include(p => p.Tutor)
            .FirstOrDefaultAsync(p => p.Id == req.PetId && p.TenantId == _t.TenantId);
        if (pet == null) return NotFound("Pet nao encontrado.");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _t.TenantId);
        if (tenant == null) return NotFound("Clinica nao encontrada.");

        string? vetNome = null, vetCrmv = null, vetMapa = null, vetTel = null, vetEmail = null;
        if (req.FuncionarioId.HasValue)
        {
            var vet = await _db.Funcionarios
                .FirstOrDefaultAsync(f => f.Id == req.FuncionarioId.Value && f.TenantId == _t.TenantId);
            if (vet != null)
            {
                vetNome  = vet.Nome;
                vetCrmv  = vet.Crmv;
                vetMapa  = vet.RegistroMapa;
                vetTel   = vet.Telefone;
                vetEmail = vet.Email;
            }
        }

        var logoBytes = await _pdf.BaixarLogo(tenant.LogoUrl);

        var tutorEndereco = new[] { pet.Tutor?.Logradouro, pet.Tutor?.NumEnd, pet.Tutor?.Bairro }
            .Where(s => !string.IsNullOrEmpty(s)).FirstOrDefault()
            ?? pet.Tutor?.Endereco;

        var sexoDisplay = pet.Sexo switch { "macho" => "Macho", "femea" => "Femea", _ => null };

        var data = new ReceituarioData
        {
            ClinicaNome     = tenant.Nome,
            ClinicaTagline  = tenant.Tagline,
            ClinicaTelefone = tenant.Telefone,
            ClinicaEmail    = tenant.Email,
            LogoBytes       = logoBytes,
            VetNome         = vetNome,
            VetCrmv         = vetCrmv,
            VetRegistroMapa = vetMapa,
            VetTelefone     = vetTel,
            VetEmail        = vetEmail,
            PetNome         = pet.Nome,
            PetCodigo       = pet.CodigoSequencial?.ToString() ?? pet.Id.ToString()[..8].ToUpper(),
            PetEspecie      = CapFirst(pet.Especie),
            PetRaca         = pet.Raca,
            PetSexo         = sexoDisplay,
            PetIdade        = PetsController.CalcularIdade(pet.DataNascimento),
            TutorNome       = pet.Tutor?.Nome ?? "---",
            TutorCpf        = pet.Tutor?.CpfCnpj,
            TutorEndereco   = tutorEndereco,
            Data            = DateTime.Now,
            TipoReceita     = req.TipoReceita ?? "Receita Veterinaria",
            ViaUso          = req.ViaUso,
            TipoFarmacia    = req.TipoFarmacia,
            Motivo          = req.Motivo,
            Observacoes     = req.Observacoes,
            Medicamentos    = req.Medicamentos.Select(m => new MedicamentoItem
            {
                Nome         = m.Nome,
                Apresentacao = m.Apresentacao,
                Dosagem      = m.Dosagem,
                Frequencia   = m.Frequencia,
                Duracao      = m.Duracao,
                Via          = m.Via,
                Quantidade   = m.Quantidade
            }).ToList()
        };

        byte[] pdfBytes = _pdf.Gerar(data);

        var linhasReceita = req.Medicamentos.Select((m, i) =>
            $"{i + 1}. {m.Nome}" +
            (!string.IsNullOrEmpty(m.Apresentacao) ? $", {m.Apresentacao}" : "") +
            (!string.IsNullOrEmpty(m.Dosagem)      ? $" | {m.Dosagem}"      : "") +
            (!string.IsNullOrEmpty(m.Frequencia)   ? $" | {m.Frequencia}"   : "") +
            (!string.IsNullOrEmpty(m.Duracao)      ? $" | {m.Duracao}"      : "") +
            (!string.IsNullOrEmpty(m.Via)          ? $" | via {m.Via}"      : "")
        );

        var vetInfo = vetNome != null
            ? $"\nVeterinario: {vetNome}{(vetCrmv != null ? $" (CRMV {vetCrmv})" : "")}"
            : "";

        var descricao =
            $"Receituario emitido em {DateTime.Now:dd/MM/yyyy HH:mm}\n" +
            (req.TipoReceita != null ? $"Tipo: {req.TipoReceita}\n" : "") +
            (req.Motivo != null ? $"Motivo: {req.Motivo}\n\n" : "\n") +
            string.Join("\n", linhasReceita) +
            (req.Observacoes != null ? $"\n\nObs: {req.Observacoes}" : "") +
            vetInfo;

        _db.ProntuarioItens.Add(new ProntuarioItem
        {
            Id          = Guid.NewGuid(),
            PetId       = pet.Id,
            UserId      = _t.UserId,
            Data        = DateTime.UtcNow,
            Tipo        = "receita",
            Titulo      = string.IsNullOrEmpty(req.Motivo) ? "Receituario" : $"Receituario - {req.Motivo}",
            Motivo      = req.Motivo,
            Descricao   = descricao,
            Receituario = string.Join("\n", linhasReceita),
            CriadoEm   = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        bool whatsappEnviado = false;
        if (req.EnviarWhatsApp && pet.Tutor?.Telefone != null)
        {
            var linhasMeds = string.Join("\n", req.Medicamentos.Select((m, i) =>
                $"{i + 1}. {m.Nome}" +
                (!string.IsNullOrEmpty(m.Dosagem)      ? $" - {m.Dosagem}"    : "") +
                (!string.IsNullOrEmpty(m.Frequencia)   ? $" | {m.Frequencia}" : "") +
                (!string.IsNullOrEmpty(m.Duracao)      ? $" | {m.Duracao}"    : "")
            ));
            var vetLinha = vetNome != null
                ? $"\n\nVeterinario: {vetNome}{(vetCrmv != null ? $" | CRMV {vetCrmv}" : "")}"
                : "";
            var msg = $"*{tenant.Nome}*\n\n" +
                $"Ola, {pet.Tutor.Nome}! Segue o receituario de {pet.Nome}.\n\n" +
                $"*Medicamentos:*\n{linhasMeds}" +
                vetLinha +
                "\n\nDuvidas? Entre em contato.";
            whatsappEnviado = await _zapi.EnviarTexto(pet.Tutor.Telefone, msg);
        }

        var nomeArq = $"receituario_{pet.Nome.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
        Response.Headers.Append("X-WhatsApp-Enviado", whatsappEnviado.ToString().ToLower());
        return File(pdfBytes, "application/pdf", nomeArq);
    }

    [HttpPost("reimprimir")]
    public async Task<IActionResult> Reimprimir(ReimprimirRequest req)
    {
        var item = await _db.ProntuarioItens
            .FirstOrDefaultAsync(p => p.Id == req.ProntuarioItemId);
        if (item == null) return NotFound("Registro nao encontrado.");

        var pet = await _db.Pets.Include(p => p.Tutor)
            .FirstOrDefaultAsync(p => p.Id == item.PetId && p.TenantId == _t.TenantId);
        if (pet == null) return NotFound("Pet nao encontrado.");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _t.TenantId);
        if (tenant == null) return NotFound();

        string? vetNome = null, vetCrmv = null, vetMapa = null, vetTel = null, vetEmail = null;
        if (item.UserId.HasValue)
        {
            var func = await _db.Funcionarios
                .FirstOrDefaultAsync(f => f.TenantId == _t.TenantId &&
                    _db.Users.Any(u => u.Id == item.UserId.Value && u.Nome == f.Nome));
            if (func != null)
            {
                vetNome  = func.Nome;
                vetCrmv  = func.Crmv;
                vetMapa  = func.RegistroMapa;
                vetTel   = func.Telefone;
                vetEmail = func.Email;
            }
        }

        var logoBytes = await _pdf.BaixarLogo(tenant.LogoUrl);

        var tutorEndereco = new[] { pet.Tutor?.Logradouro, pet.Tutor?.NumEnd, pet.Tutor?.Bairro }
            .Where(s => !string.IsNullOrEmpty(s)).FirstOrDefault()
            ?? pet.Tutor?.Endereco;

        var sexoDisplay = pet.Sexo switch { "macho" => "Macho", "femea" => "Femea", _ => null };

        // Parse medicamentos do texto salvo
        // Formato salvo: "1. Nome, Apresentacao | Dosagem | Frequencia | Duracao | via Via"
        var medicamentos = new List<MedicamentoItem>();
        if (!string.IsNullOrEmpty(item.Receituario))
        {
            var linhasSalvas = item.Receituario.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var linha in linhasSalvas)
            {
                var semNum = System.Text.RegularExpressions.Regex.Replace(linha, @"^\d+\.\s*", "").Trim();
                var partes = semNum.Split(" | ");
                var med = new MedicamentoItem { Nome = partes[0].Trim() };
                foreach (var p in partes.Skip(1))
                {
                    var v = p.Trim();
                    if (v.StartsWith("via "))          med.Via        = v[4..].Trim();
                    else if (med.Dosagem == null)       med.Dosagem    = v;
                    else if (med.Frequencia == null)    med.Frequencia = v;
                    else                                med.Duracao    = v;
                }
                medicamentos.Add(med);
            }
        }

        var dataReceit = new ReceituarioData
        {
            ClinicaNome     = tenant.Nome,
            ClinicaTagline  = tenant.Tagline,
            ClinicaTelefone = tenant.Telefone,
            ClinicaEmail    = tenant.Email,
            LogoBytes       = logoBytes,
            VetNome         = vetNome,
            VetCrmv         = vetCrmv,
            VetRegistroMapa = vetMapa,
            VetTelefone     = vetTel,
            VetEmail        = vetEmail,
            PetNome         = pet.Nome,
            PetCodigo       = pet.CodigoSequencial?.ToString() ?? pet.Id.ToString()[..8].ToUpper(),
            PetEspecie      = CapFirst(pet.Especie),
            PetRaca         = pet.Raca,
            PetSexo         = sexoDisplay,
            PetIdade        = PetsController.CalcularIdade(pet.DataNascimento),
            TutorNome       = pet.Tutor?.Nome ?? "---",
            TutorCpf        = pet.Tutor?.CpfCnpj,
            TutorEndereco   = tutorEndereco,
            Data            = item.Data.ToLocalTime(),
            TipoReceita     = "Receita Veterinaria",
            Motivo          = item.Motivo,
            Medicamentos    = medicamentos
        };

        byte[] pdfBytes = _pdf.Gerar(dataReceit);

        bool whatsappEnviado = false;
        if (req.EnviarWhatsApp && pet.Tutor?.Telefone != null && medicamentos.Any())
        {
            var linhasMeds = string.Join("\n", medicamentos.Select((m, i) =>
                $"{i + 1}. {m.Nome}" +
                (m.Dosagem    != null ? $" - {m.Dosagem}"    : "") +
                (m.Frequencia != null ? $" | {m.Frequencia}" : "") +
                (m.Duracao    != null ? $" | {m.Duracao}"    : "")
            ));
            var vetLinha = vetNome != null
                ? $"\n\nVeterinario: {vetNome}{(vetCrmv != null ? $" | CRMV {vetCrmv}" : "")}"
                : "";
            var msg = $"*{tenant.Nome}*\n\n" +
                $"Ola, {pet.Tutor.Nome}! Segue o receituario de {pet.Nome} (2a via).\n\n" +
                $"*Medicamentos:*\n{linhasMeds}" +
                vetLinha +
                "\n\nDuvidas? Entre em contato.";
            whatsappEnviado = await _zapi.EnviarTexto(pet.Tutor.Telefone, msg);
        }

        var nomeArq = $"receituario_{pet.Nome.Replace(" ", "_")}_{item.Data:yyyyMMdd}.pdf";
        Response.Headers.Append("X-WhatsApp-Enviado", whatsappEnviado.ToString().ToLower());
        return File(pdfBytes, "application/pdf", nomeArq);
    }

    private static string CapFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
