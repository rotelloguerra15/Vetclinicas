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
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    private readonly IHttpClientFactory _http;

    public IaController(AppDbContext db, TenantContext t, IHttpClientFactory http)
    {
        _db   = db;
        _t    = t;
        _http = http;
    }

    // ── GET /api/ia/status ────────────────────────────────────────────────────
    // Retorna se a IA está ativa para o tenant (sem expor a chave)

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        return Ok(new {
            ativo = p != null && p.IaAtivo && !string.IsNullOrWhiteSpace(p.AnthropicApiKey)
        });
    }

    // ── POST /api/ia/diagnostico ──────────────────────────────────────────────
    // Recebe sintomas + dados do pet, chama Claude, retorna streaming SSE

    public record DiagnosticoRequest(
        string Sintomas,        // texto livre do veterinário / transcrição do microfone
        string? PetNome,
        string? Especie,
        string? Raca,
        string? Idade,
        decimal? PesoKg
    );

    [HttpPost("diagnostico")]
    public async Task DiagnosticoStreaming(DiagnosticoRequest req, CancellationToken ct)
    {
        // Busca chave do tenant
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId, ct);

        if (p == null || !p.IaAtivo || string.IsNullOrWhiteSpace(p.AnthropicApiKey))
        {
            Response.StatusCode = 403;
            await Response.WriteAsync("IA nao configurada para este tenant.", ct);
            return;
        }

        // Monta contexto do paciente
        var paciente = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(req.PetNome))   paciente.Append($"Nome: {req.PetNome}. ");
        if (!string.IsNullOrWhiteSpace(req.Especie))   paciente.Append($"Espécie: {req.Especie}. ");
        if (!string.IsNullOrWhiteSpace(req.Raca))      paciente.Append($"Raça: {req.Raca}. ");
        if (!string.IsNullOrWhiteSpace(req.Idade))     paciente.Append($"Idade: {req.Idade}. ");
        if (req.PesoKg.HasValue)                       paciente.Append($"Peso: {req.PesoKg} kg. ");

        var userMessage = paciente.Length > 0
            ? $"Paciente: {paciente}\n\nRelato clínico: {req.Sintomas}"
            : req.Sintomas;

        const string systemPrompt =
            "Você é um assistente clínico veterinário especializado em diagnóstico. " +
            "Receberá a descrição de um paciente animal com seus sintomas e histórico. " +
            "Responda sempre em português brasileiro, de forma estruturada com:\n" +
            "**1. Diagnósticos diferenciais** (do mais ao menos provável)\n" +
            "**2. Exames recomendados**\n" +
            "**3. Conduta inicial sugerida**\n\n" +
            "Seja objetivo e técnico — o destinatário é um médico veterinário. " +
            "Não substitua a avaliação clínica presencial. Máximo 300 palavras.";

        // Monta payload para API Anthropic com streaming
        var payload = new
        {
            model      = "claude-sonnet-4-20250514",
            max_tokens = 1024,
            stream     = true,
            system     = systemPrompt,
            messages   = new[] { new { role = "user", content = userMessage } }
        };

        var json = JsonSerializer.Serialize(payload);

        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", p.AnthropicApiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Configura response como SSE
        Response.Headers["Content-Type"]  = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            using var httpResp = await client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!httpResp.IsSuccessStatusCode)
            {
                var erro = await httpResp.Content.ReadAsStringAsync(ct);
                await Response.WriteAsync($"data: {{\"erro\": \"Erro na API Anthropic: {httpResp.StatusCode}\"}}\n\n", ct);
                return;
            }

            await using var stream = await httpResp.Content.ReadAsStreamAsync(ct);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (!line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var type = doc.RootElement.GetProperty("type").GetString();

                    if (type == "content_block_delta")
                    {
                        // Anthropic streaming: delta.type = "text_delta", delta.text = "..."
                        var deltaEl = doc.RootElement.GetProperty("delta");
                        var deltaType = deltaEl.TryGetProperty("type", out var dt) ? dt.GetString() : null;

                        if (deltaType == "text_delta")
                        {
                            var delta = deltaEl.TryGetProperty("text", out var txt) ? txt.GetString() : null;
                            if (!string.IsNullOrEmpty(delta))
                            {
                                var escaped = JsonSerializer.Serialize(delta);
                                await Response.WriteAsync($"data: {escaped}\n\n", ct);
                                await Response.Body.FlushAsync(ct);
                            }
                        }
                    }
                }
                catch { /* ignora linhas malformadas */ }
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // cliente desconectou — normal
        }
    }

    // ── PUT /api/ia/configurar ────────────────────────────────────────────────
    // Salva chave API + toggle ativo

    public record IaConfigRequest(string? AnthropicApiKey, bool IaAtivo);

    [HttpPut("configurar")]
    public async Task<IActionResult> Configurar(IaConfigRequest dto)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);

        if (p == null)
            return NotFound(new { erro = "Parâmetros não encontrados. Acesse Configurações primeiro." });

        // Só atualiza a chave se vier preenchida (não sobrescreve com vazio)
        if (!string.IsNullOrWhiteSpace(dto.AnthropicApiKey))
            p.AnthropicApiKey = dto.AnthropicApiKey.Trim();

        p.IaAtivo      = dto.IaAtivo;
        p.AtualizadoEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Configuração de IA salva.", ativo = p.IaAtivo });
    }

    // ── DELETE /api/ia/chave ──────────────────────────────────────────────────
    // Remove a chave (segurança)

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
