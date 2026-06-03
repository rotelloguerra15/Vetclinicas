using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VetClinica.API.Data;
using VetClinica.API.DTOs;

namespace VetClinica.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public AuthService(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task<LoginResponse?> Login(LoginRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.Ativo);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Senha, user.SenhaHash))
            return null;

        // bloqueia acesso se a clínica estiver inativa ou suspensa (ex.: inadimplência)
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);
        if (tenant == null || !tenant.Ativo || tenant.SuspensoEm != null)
            return null;

        var token = GerarToken(user.Id, user.TenantId, user.Papel);
        return new LoginResponse(token, user.Nome, user.Papel, user.TenantId);
    }

    // Login do super-admin da plataforma (você). Não pertence a nenhuma clínica.
    public async Task<LoginResponse?> LoginPlataforma(LoginRequest req)
    {
        var admin = await _db.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Email == req.Email && a.Ativo);

        if (admin == null || !BCrypt.Net.BCrypt.Verify(req.Senha, admin.SenhaHash))
            return null;

        var token = GerarTokenPlataforma(admin.Id);
        return new LoginResponse(token, admin.Nome, "superadmin", Guid.Empty);
    }

    private string GerarTokenPlataforma(Guid adminId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
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

    private string GerarToken(Guid userId, Guid tenantId, string papel)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("papel", papel)
        };

        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(int.Parse(_cfg["Jwt:ExpireHours"] ?? "12")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
