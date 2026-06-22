using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;

namespace VetClinica.API.Services;

// Worker em background: a cada hora, suspende automaticamente clinicas
// no plano "trial" cujo trial_expira_em ja passou. Login.cs ja trata
// tenant suspenso e devolve 402 -> frontend redireciona para /planos.
public class TrialSuspensorWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<TrialSuspensorWorker> _log;
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    public TrialSuspensorWorker(IServiceProvider sp, ILogger<TrialSuspensorWorker> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), ct); // nao competir com a subida da app
        while (!ct.IsCancellationRequested)
        {
            try { await Rodar(ct); }
            catch (Exception ex) { _log.LogError(ex, "Erro no TrialSuspensorWorker"); }
            await Task.Delay(Intervalo, ct);
        }
    }

    private async Task Rodar(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var agora = DateTime.UtcNow;
        var vencidos = await platform.Tenants
            .Where(t => t.Ativo
                     && t.SuspensoEm == null
                     && t.Plano == "trial"
                     && t.TrialExpiraEm != null
                     && t.TrialExpiraEm.Value <= agora)
            .ToListAsync(ct);

        if (vencidos.Count == 0) return;

        foreach (var t in vencidos)
            t.SuspensoEm = agora;

        await platform.SaveChangesAsync(ct);
        _log.LogInformation("TrialSuspensorWorker: suspendeu {N} clinica(s) com trial vencido: {Nomes}",
            vencidos.Count, string.Join(", ", vencidos.Select(t => t.Nome)));
    }
}
