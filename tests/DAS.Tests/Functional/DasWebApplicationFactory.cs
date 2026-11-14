using System.Text.RegularExpressions;
using DAS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DAS.Tests.Functional;

//Levanta la aplicación completa (MVC, Identity, EF Core) con una base SQLite en memoria propia
public sealed class DasWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=das-pruebas-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public DasWebApplicationFactory()
    {
        //La base en memoria existe mientras haya al menos una conexión abierta
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(options => options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName);
        });
    }

    public HttpClient CreateClientAs(string? role)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        }

        return client;
    }

    public async Task<T> QueryAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(context);
    }

    public Task<decimal> StockOfAsync(string productName) =>
        QueryAsync(async context => (await context.Products.AsNoTracking().SingleAsync(product => product.Name == productName)).Stock);

    public Task<int> ProductIdAsync(string productName) =>
        QueryAsync(async context => (await context.Products.AsNoTracking().SingleAsync(product => product.Name == productName)).Id);

    //Los formularios POST exigen el token antifalsificación que la vista incluye en cada formulario
    public static async Task<string> AntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, $"La página {url} no incluye el token antifalsificación");
        return match.Groups[1].Value;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
