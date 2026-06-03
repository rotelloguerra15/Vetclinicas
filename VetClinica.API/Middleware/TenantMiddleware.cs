using System.Security.Claims;

namespace VetClinica.API.Middleware;

// Reaproveitado da lavanderia: extrai tenant_id e user_id dos claims do JWT
// e popula o TenantContext do escopo atual.
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, TenantContext tenant)
    {
        var tenantClaim = ctx.User.FindFirst("tenant_id")?.Value;
        var userClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var papelClaim = ctx.User.FindFirst("papel")?.Value;

        if (Guid.TryParse(tenantClaim, out var tid))
        {
            tenant.TenantId = tid;
            tenant.Resolved = true;
        }
        if (Guid.TryParse(userClaim, out var uid)) tenant.UserId = uid;
        tenant.Papel = papelClaim ?? "";
        tenant.IsPlatformAdmin = ctx.User.FindFirst("platform_admin")?.Value == "true";

        await _next(ctx);
    }
}
