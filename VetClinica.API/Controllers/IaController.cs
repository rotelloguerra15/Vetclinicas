using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using VetClinica.API.Data;
using VetClinica.API.Middleware;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ia")]
public class IaController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext   _t;
    private readonly IHttpClientFactory _http;

    public IaController(TenantDbContextFactory factory, TenantContext t, IHttpClientFactory http)
    {
        _db   = factory.Create();
        _t    = t;
        _http = http;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);
        return Ok(new {
            ativo = p != null && p.IaAtivo && !string.IsNullOrWhiteSpace(p.AnthropicApiKey)
        });
    }

    public record DiagnosticoRequest(
        string  Sintomas,
        string? PetNome,
        string? Especie,
        string? Raca,
        string? Idade,
        decimal? PesoKg
    );

    // Endpoint original — retorna texto livre
    [HttpPost("diagnostico")]
    public async Task<IActionResult> Diagnostico(DiagnosticoRequest req)
    {
        var (p, err) = await GetParametros();
        if (err != null) return err;

        var userMessage = MontarMensagemPaciente(req);
        var systemPrompt =
            "Voce e um assistente clinico veterinario especializado em diagnostico. " +
            "Recebera a descricao de um paciente animal com seus sintomas e historico. " +
            "Responda sempre em portugues brasileiro, de forma estruturada com:\n" +
            "1. Diagnosticos diferenciais (do mais ao menos provavel)\n" +
            "2. Exames recomendados\n" +
            "3. Conduta inicial sugerida\n\n" +
            "Seja objetivo e tecnico. O destinatario e um medico veterinario. " +
            "Nao substitua a avaliacao clinica presencial. Maximo 300 palavras.";

        var resultado = await ChamarClaude(p!.AnthropicApiKey!, systemPrompt, userMessage);
        if (resultado is IActionResult ar) return ar;
        return Ok(new { texto = resultado as string });
    }

    // Novo endpoint — retorna JSON estruturado com diagnostico + medicamentos
    [HttpPost("diagnostico-receituario")]
    public async Task<IActionResult> DiagnosticoReceituario(DiagnosticoRequest req)
    {
        var (p, err) = await GetParametros();
        if (err != null) return err;

        var userMessage = MontarMensagemPaciente(req);
        var systemPrompt =
            "Voce e um assistente clinico veterinario especializado em diagnostico e prescricao. " +
            "Recebera a descricao de um paciente animal com seus sintomas. " +
            "Responda EXCLUSIVAMENTE com um JSON valido, sem texto antes ou depois, sem markdown, sem backticks. " +
            "O JSON deve ter exatamente este formato:\n" +
            "{\n" +
            "  \"diagnostico\": \"texto com diagnostico diferencial e conduta (ate 300 palavras)\",\n" +
            "  \"medicamentos\": [\n" +
            "    {\n" +
            "      \"nome\": \"Nome do medicamento\",\n" +
            "      \"apresentacao\": \"Forma farmaceutica e concentracao\",\n" +
            "      \"dosagem\": \"Dose por administracao\",\n" +
            "      \"frequencia\": \"Ex: 2x ao dia\",\n" +
            "      \"duracao\": \"Ex: 7 dias\",\n" +
            "      \"via\": \"Ex: oral, subcutanea, topica\",\n" +
            "      \"quantidade\": \"Total de unidades a dispensar\"\n" +
            "    }\n" +
            "  ]\n" +
            "}\n" +
            "Sugira de 1 a 4 medicamentos pertinentes ao caso. " +
            "Se nao houver medicamento indicado, retorne array vazio. " +
            "O destinatario e um medico veterinario. " +
            "IMPORTANTE: retorne APENAS o JSON, nada mais.";

        var resultado = await ChamarClaude(p!.AnthropicApiKey!, systemPrompt, userMessage);
        if (resultado is IActionResult ar2) return ar2;

        var textoJson = ((resultado as string) ?? "").Trim();

        // Limpa possíveis marcadores markdown
        if (textoJson.StartsWith("```"))
        {
            var linhas = textoJson.Split('\n').Skip(1).ToList();
            textoJson = string.Join("\n", linhas.TakeWhile(l => !l.TrimStart().StartsWith("```")));
        }

        try
        {
            using var doc = JsonDocument.Parse(textoJson);
            var diagnostico  = doc.RootElement.GetProperty("diagnostico").GetString();
            var meds         = doc.RootElement.GetProperty("medicamentos").GetRawText();
            return Ok(new { diagnostico, medicamentos = JsonSerializer.Deserialize<object>(meds) });
        }
        catch
        {
            return Ok(new { diagnostico = textoJson, medicamentos = Array.Empty<object>() });
        }
    }

    public record IaConfigRequest(string? AnthropicApiKey, bool IaAtivo);

    [HttpPut("configurar")]
    public async Task<IActionResult> Configurar(IaConfigRequest dto)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null)
            return NotFound(new { erro = "Parametros nao encontrados." });

        if (!string.IsNullOrWhiteSpace(dto.AnthropicApiKey))
            p.AnthropicApiKey = dto.AnthropicApiKey.Trim();

        p.IaAtivo      = dto.IaAtivo;
        p.AtualizadoEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Configuracao de IA salva.", ativo = p.IaAtivo });
    }

    [HttpDelete("chave")]
    public async Task<IActionResult> RemoverChave()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null) return NotFound();

        p.AnthropicApiKey = null;
        p.IaAtivo         = false;
        p.AtualizadoEm    = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Chave removida." });
    }

    // Helpers
    private async Task<(VetClinica.API.Models.ParametrosSistema? p, IActionResult? err)> GetParametros()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null || !p.IaAtivo || string.IsNullOrWhiteSpace(p.AnthropicApiKey))
            return (null, StatusCode(403, new { erro = "IA nao configurada para este tenant." }));

        return (p, null);
    }

    private static string MontarMensagemPaciente(DiagnosticoRequest req)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(req.PetNome)) sb.Append($"Nome: {req.PetNome}. ");
        if (!string.IsNullOrWhiteSpace(req.Especie))  sb.Append($"Especie: {req.Especie}. ");
        if (!string.IsNullOrWhiteSpace(req.Raca))     sb.Append($"Raca: {req.Raca}. ");
        if (!string.IsNullOrWhiteSpace(req.Idade))    sb.Append($"Idade: {req.Idade}. ");
        if (req.PesoKg.HasValue)                      sb.Append($"Peso: {req.PesoKg} kg. ");

        return sb.Length > 0
            ? $"Paciente: {sb}\n\nRelato clinico: {req.Sintomas}"
            : req.Sintomas;
    }

    private async Task<object> ChamarClaude(string apiKey, string systemPrompt, string userMessage)
    {
        var payload = new
        {
            model      = "claude-haiku-4-5-20251001",
            max_tokens = 1024,
            stream     = false,
            system     = systemPrompt,
            messages   = new[] { new { role = "user", content = userMessage } }
        };

        var json   = JsonSerializer.Serialize(payload);
        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        client.Timeout = TimeSpan.FromSeconds(60);

        try
        {
            var httpResp = await client.PostAsync(
                "https://api.anthropic.com/v1/messages",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var body = await httpResp.Content.ReadAsStringAsync();

            if (!httpResp.IsSuccessStatusCode)
                return StatusCode(502, new { erro = $"Erro Anthropic: {httpResp.StatusCode}", detalhe = body });

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new { erro = "Timeout. Tente novamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { erro = ex.Message });
        }
    }
}
