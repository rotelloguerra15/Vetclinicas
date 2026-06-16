using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;

namespace VetClinica.API.Services;

public class AgendaService
{
    private readonly TenantDbContext _db;
    public AgendaService(TenantDbContextFactory factory) { _db = factory.Create(); }

    private static readonly TimeOnly AberturaDefault  = new(8, 0);
    private static readonly TimeOnly FechamentoDefault = new(18, 0);

    /// <summary>
    /// Retorna slots livres respeitando a duracao do servico.
    /// Se duracaoMin nao informado, usa 60 min como padrao.
    /// </summary>
    public async Task<List<SlotLivre>> GetSlotsLivres(
        Guid tenantId, DateTime inicio, int dias, int duracaoMin = 60)
    {
        // Tenta ler horario de funcionamento da config do bot (se existir)
        var botCfg = await _db.BotConfigs
            .FirstOrDefaultAsync(b => b.TenantId == tenantId);

        var abertura   = botCfg?.HoraInicio ?? AberturaDefault;
        var fechamento = botCfg?.HoraFim    ?? FechamentoDefault;

        var fim = inicio.AddDays(dias);

        var ocupados = await _db.Agendamentos
            .Where(a => a.TenantId == tenantId
                     && a.DataHora >= inicio
                     && a.DataHora < fim
                     && a.Status != "cancelado"
                     && a.Status != "faltou")
            .Select(a => new { a.DataHora, a.DuracaoMin })
            .ToListAsync();

        var diasPermitidos = (botCfg?.DiasSemana ?? "1,2,3,4,5,6")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => int.TryParse(d, out var n) ? n : -1)
            .ToHashSet();

        var slots = new List<SlotLivre>();

        for (var dia = inicio.Date; dia < fim.Date; dia = dia.AddDays(1))
        {
            var diaSemana = (int)dia.DayOfWeek; // 0=dom, 6=sab
            if (!diasPermitidos.Contains(diaSemana)) continue;

            var hora   = dia.Add(abertura.ToTimeSpan());
            var limite = dia.Add(fechamento.ToTimeSpan());

            while (hora.AddMinutes(duracaoMin) <= limite)
            {
                var slotFim = hora.AddMinutes(duracaoMin);

                bool conflito = ocupados.Any(o =>
                    hora < o.DataHora.AddMinutes(o.DuracaoMin) &&
                    slotFim > o.DataHora);

                if (!conflito && hora > DateTime.Now)
                    slots.Add(new SlotLivre(hora, slotFim));

                hora = slotFim;
            }
        }

        return slots;
    }
}
