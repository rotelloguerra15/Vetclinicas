namespace VetClinica.API.Middleware;

// Carrega o tenant_id + user_id da requisição atual (vindo do JWT).
// Injetado como Scoped — uma instância por request.
public class TenantContext
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Papel { get; set; } = "";
    public bool Resolved { get; set; }
    public bool IsPlatformAdmin { get; set; }
}
