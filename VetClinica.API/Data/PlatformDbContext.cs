using Microsoft.EntityFrameworkCore;
using VetClinica.API.Models;

namespace VetClinica.API.Data;

// Contexto da plataforma (Ketra / Rotello).
// Schema "platform" — só metadados: lista de clínicas e super-admins.
public class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options) { }

    public DbSet<Tenant>        Tenants        => Set<Tenant>();
    public DbSet<PlatformAdmin>  PlatformAdmins  => Set<PlatformAdmin>();
    public DbSet<VetClinica.API.Models.Configuracao> Configuracoes => Set<VetClinica.API.Models.Configuracao>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("platform");
        base.OnModelCreating(mb);
    }
}
