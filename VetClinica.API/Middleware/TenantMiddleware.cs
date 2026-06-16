using System.Security.Claims;

namespace VetClinica.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, TenantContext tenant)
    {
        var tenantClaim = ctx.User.FindFirst("tenant_id")?.Value;
        var userClaim   = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var papelClaim  = ctx.User.FindFirst("papel")?.Value;
        var schemaClaim = ctx.User.FindFirst("schema")?.Value;

        if (Guid.TryParse(tenantClaim, out var tid)) { tenant.TenantId = tid; tenant.Resolved = true; }
        if (Guid.TryParse(userClaim,   out var uid))   tenant.UserId = uid;

        tenant.Papel           = papelClaim  ?? "";
        tenant.SchemaName      = schemaClaim ?? "";
        tenant.IsPlatformAdmin = ctx.User.FindFirst("platform_admin")?.Value == "true";

        await _next(ctx);
    }
}
