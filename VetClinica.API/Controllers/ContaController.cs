using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.DTOs;
using VetClinica.API.Middleware;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

// So permite o usuario logado trocar a PROPRIA senha -- nunca cria usuario,
// nunca altera outro. Exige a senha atual (diferente do "esqueci senha",
// que e pra quem nao consegue logar).
[ApiController]
[Authorize]
[Route("api/conta")]
public class ContaController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly TenantContext   _t;
    private readonly IEmailService   _email;

    public ContaController(TenantDbContextFactory factory, TenantContext t, IEmailService email)
    { _db = factory.Create(); _t = t; _email = email; }

    [HttpPost("trocar-senha")]
    public async Task<IActionResult> TrocarSenha(TrocarSenhaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NovaSenha) || req.NovaSenha.Length < 6)
            return BadRequest(new { erro = "A nova senha deve ter pelo menos 6 caracteres." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _t.UserId && u.Ativo);
        if (user == null) return NotFound(new { erro = "Usuario nao encontrado." });

        if (!BCrypt.Net.BCrypt.Verify(req.SenhaAtual, user.SenhaHash))
            return BadRequest(new { erro = "Senha atual incorreta." });

        user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha, workFactor: 11);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var html = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
  <div style='background: #0f172a; padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
    <h1 style='color: white; margin: 0;'>VetClinica</h1>
  </div>
  <div style='background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0;'>
    <h2>Sua senha foi alterada</h2>
    <p>Confirmamos a alteracao da senha da conta <strong>{user.Email}</strong> agora ha pouco.</p>
    <p style='font-size: 13px; color: #94a3b8;'>Se voce nao fez essa alteracao, entre em contato com o suporte imediatamente: <a href='mailto:suporte@ketra.com.br'>suporte@ketra.com.br</a>.</p>
  </div>
</body></html>";
            await _email.EnviarAsync(user.Email, "Sua senha foi alterada - VetClinica", html);
        }

        return Ok(new { mensagem = "Senha alterada com sucesso." });
    }
}
