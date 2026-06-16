using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
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
    private readonly TenantContext _t;
    private readonly IHttpClientFactory _http;

    public IaController(TenantDbContextFactory factory, TenantContext t, IHttpClientFactory http)
    {
        _db   = db;
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
        string Sintomas,
        string? PetNome,
        string? Especie,
        string? Raca,
        string? Idade,
        decimal? PesoKg
    );

    [HttpPost("diagnostico")]
    public async Task<IActionResult> Diagnostico(DiagnosticoRequest req)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null || !p.IaAtivo || string.IsNullOrWhiteSpace(p.AnthropicApiKey))
            return StatusCode(403, new { erro = "IA nao configurada para este tenant." });

        var paciente = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(req.PetNome)) paciente.Append($"Nome: {req.PetNome}. ");
        if (!string.IsNullOrWhiteSpace(req.Especie))  paciente.Append($"Especie: {req.Especie}. ");
        if (!string.IsNullOrWhiteSpace(req.Raca))     paciente.Append($"Raca: {req.Raca}. ");
        if (!string.IsNullOrWhiteSpace(req.Idade))    paciente.Append($"Idade: {req.Idade}. ");
        if (req.PesoKg.HasValue)                      paciente.Append($"Peso: {req.PesoKg} kg. ");

        var userMessage = paciente.Length > 0
            ? $"Paciente: {paciente}\n\nRelato clinico: {req.Sintomas}"
            : req.Sintomas;

        var systemPrompt =
            "Voce e um assistente clinico veterinario especializado em diagnostico. " +
            "Recebera a descricao de um paciente animal com seus sintomas e historico. " +
            "Responda sempre em portugues brasileiro, de forma estruturada com:\n" +
            "1. Diagnosticos diferenciais (do mais ao menos provavel)\n" +
            "2. Exames recomendados\n" +
            "3. Conduta inicial sugerida\n\n" +
            "Seja objetivo e tecnico. O destinatario e um medico veterinario. " +
            "Nao substitua a avaliacao clinica presencial. Maximo 300 palavras.";

        var payload = new
        {
            model      = "claude-haiku-4-5-20251001",
            max_tokens = 1024,
            stream     = false,
            system     = systemPrompt,
            messages   = new[] { new { role = "user", content = userMessage } }
        };

        var json = JsonSerializer.Serialize(payload);

        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", p.AnthropicApiKey);
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
            var texto = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return Ok(new { texto });
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

    public record IaConfigRequest(string? AnthropicApiKey, bool IaAtivo);

    [HttpPut("configurar")]
    public async Task<IActionResult> Configurar(IaConfigRequest dto)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null)
            return NotFound(new { erro = "Parametros nao encontrados. Acesse Configuracoes primeiro." });

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
}
