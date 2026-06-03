using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

public class AgendamentoLinkService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public AgendamentoLinkService(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task<string> GerarLinkAgendamento(Guid tenantId, Guid tutorId, Guid? petId)
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

        _db.LinksAgendamento.Add(link);
        await _db.SaveChangesAsync();

        var baseUrl = _cfg["App:FrontendUrl"];
        return $"{baseUrl}/agendar/{token}";
    }
}
