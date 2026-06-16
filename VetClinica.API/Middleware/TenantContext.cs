namespace VetClinica.API.Middleware;

// Carrega tenant_id + user_id + schema da requisição atual (JWT).
// SchemaName é o schema PostgreSQL desta clínica (ex: "vet_barbarafonseca").
public class TenantContext
{
    public Guid   TenantId      { get; set; }
    public Guid   UserId        { get; set; }
    public string Papel         { get; set; } = "";
    public bool   Resolved      { get; set; }
    public bool   IsPlatformAdmin { get; set; }
    public string SchemaName    { get; set; } = "";
}
