using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DAS.Infrastructure.Identity;
using DAS.Web.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DAS.Tests.Functional;

public static class ApiTestExtensions
{
    public const string TestPassword = "Prueba#2026x";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static HttpClient CreateHttpsClient(this WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    public static async Task EnsureUserAsync(this DasWebApplicationFactory factory, string email, string role)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            var created = await users.CreateAsync(user, TestPassword);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            var added = await users.AddToRoleAsync(user, role);
            Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(error => error.Description)));
        }
    }

    public static async Task<HttpClient> CreateJwtClientAsync(this DasWebApplicationFactory factory, string role)
    {
        var email = $"jwt-{role.ToLowerInvariant()}@pruebas.local";
        await factory.EnsureUserAsync(email, role);

        var client = factory.CreateClientAs(null);
        var response = await client.PostAsJsonAsync("/api/auth/token", new { email, password = TestPassword });
        response.EnsureSuccessStatusCode();
        var token = await response.ReadJsonElementAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("access_token").GetString());
        return client;
    }

    public static async Task<string> CsrfTokenAsync(this HttpClient client)
    {
        var html = await client.GetStringAsync("/inventario");
        var match = Regex.Match(html, "<meta name=\"csrf-token\" content=\"([^\"]+)\"");
        Assert.True(match.Success, "El layout no publica el token antifalsificación en <meta name=\"csrf-token\">");
        return match.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostJsonWithCsrfAsync(this HttpClient client, string url, object body)
    {
        var token = await client.CsrfTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add(SecurityPolicies.AntiforgeryHeaderName, token);
        return await client.SendAsync(request);
    }

    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    public static async Task<JsonElement> ReadJsonElementAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    public static IReadOnlyList<string> ErrorsFor(this JsonElement problem, string field) =>
        problem.GetProperty("errors")
            .EnumerateObject()
            .Where(property => string.Equals(property.Name.Split('.').Last(), field, StringComparison.OrdinalIgnoreCase))
            .SelectMany(property => property.Value.EnumerateArray().Select(message => message.GetString() ?? string.Empty))
            .ToList();

    public static string? MediaType(this HttpResponseMessage response) => response.Content.Headers.ContentType?.MediaType;

    public static string HeaderValue(this HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) || response.Content.Headers.TryGetValues(name, out values)
            ? string.Join(", ", values)
            : string.Empty;
}
