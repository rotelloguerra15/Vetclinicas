using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

// Usado SOMENTE pelo NotificacaoDispatcher (background worker, sem HTTP/JWT).
// Por isso recebe o TenantDbContext já resolvido por schema, em vez de
// depender do TenantDbContextFactory (que so funciona dentro de uma requisicao).
public class AgendamentoLinkService
{
    private readonly IConfiguration _cfg;

    public AgendamentoLinkService(IConfiguration cfg)
    {
        _cfg = cfg;
    }

    public async Task<string> GerarLinkAgendamento(TenantDbContext db, Guid tenantId, Guid tutorId, Guid? petId)
    {
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()
            .Concat(Guid.NewGuid().ToByteArray()).ToArray()).ToLower();

        var link = new LinkAgendamento
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TutorId = tutorId,
            PetId = petId,
            Token = token,
            ExpiraEm = DateTime.UtcNow.AddDays(7),
            CriadoEm = DateTime.UtcNow
        };

        db.LinksAgendamento.Add(link);
        await db.SaveChangesAsync();

        var baseUrl = _cfg["App:FrontendUrl"];
        return $"{baseUrl}/agendar/{token}";
    }
}

