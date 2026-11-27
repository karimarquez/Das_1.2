using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DAS.Infrastructure.Identity;
using DAS.Web.Models.Auth;
using DAS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace DAS.Web.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class ApiAuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("token")]
    [EnableRateLimiting(SecurityPolicies.CredentialsRateLimit)]
    [EndpointSummary("Obtiene un token JWT")]
    [EndpointDescription("Recibe el email y la password de una cuenta registrada y devuelve access_token, token_type (Bearer) y expires_at (UTC, una hora de vigencia). Cinco contraseñas incorrectas bloquean la cuenta 10 minutos y los intentos por minuto desde un mismo equipo están limitados.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<IActionResult> CreateToken(TokenRequest input)
    {
        var user = await userManager.FindByEmailAsync(input.Email);
        if (user is null || !await signInManager.CanSignInAsync(user))
            return Unauthorized(new { error = "Credenciales incorrectas." });

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, input.Password, lockoutOnFailure: true);
        if (!passwordResult.Succeeded)
            return Unauthorized(new { error = "Credenciales incorrectas." });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expires = DateTime.UtcNow.AddMinutes(60);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "DAS",
            audience: configuration["Jwt:Audience"] ?? "DAS.Api",
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Ok(new { access_token = new JwtSecurityTokenHandler().WriteToken(token), token_type = "Bearer", expires_at = expires });
    }
}
