using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DAS.Tests.Functional;

//Simula una sesión iniciada con el rol que indique el encabezado; sin encabezado la petición es anónima
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Pruebas";
    public const string RoleHeader = "X-Rol-Prueba";

    public static string UserNameFor(string role) => $"{role.ToLowerInvariant()}@pruebas.local";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var values) || string.IsNullOrWhiteSpace(values.ToString()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = values.ToString();
        var userName = UserNameFor(role);
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userName), new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.Role, role)],
            SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
