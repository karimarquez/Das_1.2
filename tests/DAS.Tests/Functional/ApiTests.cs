using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DAS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DAS.Tests.Functional;

public class ApiTests(DasWebApplicationFactory factory) : IClassFixture<DasWebApplicationFactory>
{
    private const string Email = "api@pruebas.local";
    private const string Password = "Prueba#2026x";

    [Fact]
    public async Task ApiSinToken_Responde401()
    {
        var response = await factory.CreateClientAs(null).GetAsync("/api/inventario/productos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenConContrasenaIncorrecta_Responde401()
    {
        await EnsureUserAsync();

        var response = await factory.CreateClientAs(null).PostAsJsonAsync("/api/auth/token", new { email = Email, password = "Incorrecta#1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenValido_PermiteConsultarExistencias()
    {
        await EnsureUserAsync();
        var client = factory.CreateClientAs(null);

        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("/api/inventario/productos?search=cerveza");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cerveza Pilsener", await response.Content.ReadAsStringAsync());
    }

    private async Task EnsureUserAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByEmailAsync(Email) is null)
        {
            var result = await users.CreateAsync(new ApplicationUser { UserName = Email, Email = Email }, Password);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
