using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinica.API.DTOs;
using VetClinica.API.Services;

namespace VetClinica.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var result = await _auth.Login(req);
        if (result == null) return Unauthorized(new { erro = "Credenciais inválidas" });
        return Ok(result);
    }

    // Login do super-admin da plataforma (Rotello)
    [HttpPost("login-plataforma")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginPlataforma(LoginRequest req)
    {
        var result = await _auth.LoginPlataforma(req);
        if (result == null) return Unauthorized(new { erro = "Credenciais inválidas" });
        return Ok(result);
    }
}
