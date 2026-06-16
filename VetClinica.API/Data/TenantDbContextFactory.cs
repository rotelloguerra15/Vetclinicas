using Microsoft.EntityFrameworkCore;
using VetClinica.API.Middleware;

namespace VetClinica.API.Data;

// Factory scoped que entrega o TenantDbContext com o schema correto.
// Injetada em todos os controllers no lugar do AppDbContext antigo.
// O schema vem do claim "schema" do JWT, lido pelo TenantMiddleware.
public class TenantDbContextFactory
{
    private readonly IConfiguration _cfg;
    private readonly TenantContext  _tenantCtx;
    private TenantDbContext?        _instance;

    public TenantDbContextFactory(IConfiguration cfg, TenantContext tenantCtx)
    {
        _cfg       = cfg;
        _tenantCtx = tenantCtx;
    }

    // Retorna (e cacheia na request) o DbContext com o schema do tenant logado.
    public TenantDbContext Create()
    {
        if (_instance != null) return _instance;

        var schema = _tenantCtx.SchemaName;
        if (string.IsNullOrWhiteSpace(schema))
            throw new InvalidOperationException(
                "SchemaName nao resolvido. Verifique o JWT claim 'schema'.");

        var connStr = _cfg.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionString 'Default' nao configurada.");

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connStr)
            .Options;

        _instance = new TenantDbContext(opts, schema);
        return _instance;
    }

    // Cria um contexto avulso para um schema específico.
    // Usado pelo ProvisionamentoService ao criar nova clínica.
    public TenantDbContext CreateForSchema(string schema)
    {
        var connStr = _cfg.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionString 'Default' nao configurada.");

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new TenantDbContext(opts, schema);
    }
}
