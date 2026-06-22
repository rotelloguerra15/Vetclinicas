using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;

namespace VetClinica.API.Services;

// Roda 1x/hora. Para clinicas com status_pagamento = "atraso":
//  - manda no maximo 1 email por dia avisando quantos dias faltam pro bloqueio
//  - ao completar 20 dias de atraso, suspende automaticamente (SuspensoEm)
//    -- a partir dai o login ja cai em 402 -> /planos, igual qualquer suspensao.
//
// "Dias em atraso" e calculado a partir de ProximoFaturamento (a data que
// venceu): o webhook do Asaas so atualiza esse campo quando confirma um
// pagamento, entao enquanto estiver em atraso ele continua apontando pra
// data que ficou pendente.
public class AssinaturaAtrasoWorker : BackgroundService
{
    private const int DiasCarencia = 20;
    private readonly IServiceProvider _sp;
    private readonly ILogger<AssinaturaAtrasoWorker> _log;
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    public AssinaturaAtrasoWorker(IServiceProvider sp, ILogger<AssinaturaAtrasoWorker> log)
    { _sp = sp; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), ct); // nao competir com a subida da app
        while (!ct.IsCancellationRequested)
        {
            try { await Rodar(ct); }
            catch (Exception ex) { _log.LogError(ex, "Erro no AssinaturaAtrasoWorker"); }
            await Task.Delay(Intervalo, ct);
        }
    }

    private async Task Rodar(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var email    = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var hoje = DateTime.UtcNow.Date;

        var emAtraso = await platform.Tenants
            .Where(t => t.Ativo
                     && t.SuspensoEm == null
                     && t.StatusPagamento == "atraso"
                     && t.ProximoFaturamento != null)
            .ToListAsync(ct);

        foreach (var t in emAtraso)
        {
            var diasAtraso = (hoje - t.ProximoFaturamento!.Value.Date).Days;
            if (diasAtraso < 0) continue; // data inconsistente -- nao faz nada, evita suspender por engano

            if (diasAtraso >= DiasCarencia)
            {
                t.SuspensoEm = DateTime.UtcNow;
                _log.LogWarning("AssinaturaAtrasoWorker: suspendeu {Nome} ({Dias} dias em atraso)", t.Nome, diasAtraso);
                continue;
            }

            // no maximo 1 aviso por dia
            if (t.UltimoAvisoAtrasoEm?.Date == hoje) continue;
            if (string.IsNullOrWhiteSpace(t.Email)) continue;

            var diasRestantes = DiasCarencia - diasAtraso;
            var html = MontarEmail(t.Nome, diasAtraso, diasRestantes);
            var r = await email.EnviarAsync(t.Email, $"Pagamento em atraso — {diasRestantes} dia(s) para o bloqueio", html);

            if (r.Ok)
            {
                t.UltimoAvisoAtrasoEm = DateTime.UtcNow;
                _log.LogInformation("AssinaturaAtrasoWorker: aviso enviado para {Email} ({Dias} dias restantes)", t.Email, diasRestantes);
            }
            else
            {
                _log.LogError("AssinaturaAtrasoWorker: falha ao enviar aviso para {Email}: {Erro}", t.Email, r.Erro);
            }
        }

        await platform.SaveChangesAsync(ct);
    }

    private static string MontarEmail(string nomeClinica, int diasAtraso, int diasRestantes) => $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
  <div style='background: #0f172a; padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
    <h1 style='color: white; margin: 0;'>VetClinica</h1>
  </div>
  <div style='background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0;'>
    <h2>Pagamento em atraso</h2>
    <p>Clinica: <strong>{nomeClinica}</strong></p>
    <p>O pagamento da assinatura esta em atraso ha {diasAtraso} dia(s).</p>
    <p style='font-size: 18px; color: #b91c1c;'><strong>Faltam {diasRestantes} dia(s)</strong> para o acesso ao sistema ser bloqueado por falta de pagamento.</p>
    <p>Regularize o pagamento o quanto antes para evitar a interrupcao do servico. Em caso de duvidas, responda este email ou fale com <a href='mailto:suporte@ketra.com.br'>suporte@ketra.com.br</a>.</p>
  </div>
</body></html>";
}
