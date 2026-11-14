using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DAS.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace DAS.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class ApiAuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("token")]
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
