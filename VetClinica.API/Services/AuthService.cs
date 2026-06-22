using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

public class AuthService
{
    private readonly PlatformDbContext _platform;
    private readonly IConfiguration   _cfg;

    public AuthService(PlatformDbContext platform, IConfiguration cfg)
    {
        _platform = platform;
        _cfg      = cfg;
    }

    public async Task<LoginOutcome> Login(LoginRequest req)
    {
        // 1. Tenta login como owner (email = email do tenant)
        // Nota: nao filtra SuspensoEm aqui de proposito -- precisamos achar o tenant
        // suspenso para devolver um motivo claro (trial vencido / pagamento) em vez
        // de simplesmente dizer "credenciais invalidas".
        var tenantRecord = await _platform.Tenants
            .FirstOrDefaultAsync(t => t.Email == req.Email
                                   && t.Ativo
                                   && t.SchemaName != null);

        if (tenantRecord != null)
        {
            using var db = CriarDbParaSchema(tenantRecord.SchemaName!);
            var owner = await db.Users
                .FirstOrDefaultAsync(u => u.Email == req.Email && u.Ativo);

            if (owner == null || !BCrypt.Net.BCrypt.Verify(req.Senha, owner.SenhaHash))
                return new LoginOutcome(null, "credenciais_invalidas", null);

            if (tenantRecord.SuspensoEm != null)
                return new LoginOutcome(null, "suspenso", MensagemSuspensao(tenantRecord));

            var token = GerarToken(owner.Id, tenantRecord.Id, owner.Papel, tenantRecord.SchemaName!);
            return new LoginOutcome(
                new LoginResponse(token, owner.Nome, owner.Papel, tenantRecord.Id, tenantRecord.Plano, tenantRecord.TrialExpiraEm),
                null, null);
        }

        // 2. Login como usuário não-owner — busca o schema varrendo os tenants ativos
        // (inclusive suspensos, pelo mesmo motivo do passo 1)
        var tenants = await _platform.Tenants
            .Where(t => t.Ativo && t.SchemaName != null)
            .ToListAsync();

        foreach (var t in tenants)
        {
            using var db = CriarDbParaSchema(t.SchemaName!);
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == req.Email && u.Ativo);

            if (user != null && BCrypt.Net.BCrypt.Verify(req.Senha, user.SenhaHash))
            {
                if (t.SuspensoEm != null)
                    return new LoginOutcome(null, "suspenso", MensagemSuspensao(t));

                var token = GerarToken(user.Id, t.Id, user.Papel, t.SchemaName!);
                return new LoginOutcome(
                    new LoginResponse(token, user.Nome, user.Papel, t.Id, t.Plano, t.TrialExpiraEm),
                    null, null);
            }
        }

        return new LoginOutcome(null, "credenciais_invalidas", null);
    }

    private static string MensagemSuspensao(Tenant t) =>
        t.Plano == "trial" && t.TrialExpiraEm != null && t.TrialExpiraEm.Value <= DateTime.UtcNow
            ? "Seu periodo de teste expirou. Escolha um plano para continuar usando o sistema."
            : "Sua assinatura esta suspensa. Escolha um plano para reativar o acesso.";

    public async Task<LoginResponse?> LoginPlataforma(LoginRequest req)
    {
        var admin = await _platform.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Email == req.Email && a.Ativo);

        if (admin == null || !BCrypt.Net.BCrypt.Verify(req.Senha, admin.SenhaHash))
            return null;

        var token = GerarTokenPlataforma(admin.Id);
        return new LoginResponse(token, admin.Nome, "superadmin", Guid.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private TenantDbContext CriarDbParaSchema(string schema)
    {
        var connStr = _cfg.GetConnectionString("Default")!;
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connStr)
            .Options;
        return new TenantDbContext(opts, schema);
    }

    private string GerarToken(Guid userId, Guid tenantId, string papel, string schema)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("papel",     papel),
            new Claim("schema",    schema)   // <- isolamento pelo schema PostgreSQL
        };
        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"], audience: _cfg["Jwt:Audience"], claims: claims,
            expires: DateTime.UtcNow.AddHours(int.Parse(_cfg["Jwt:ExpireHours"] ?? "12")),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GerarTokenPlataforma(Guid adminId)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
            new Claim("platform_admin", "true"),
            new Claim("papel", "superadmin")
        };
        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"], audience: _cfg["Jwt:Audience"], claims: claims,
            expires: DateTime.UtcNow.AddHours(int.Parse(_cfg["Jwt:ExpireHours"] ?? "12")),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
