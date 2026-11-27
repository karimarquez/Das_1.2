using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DAS.Application.Security;
using DAS.Domain.Entities;
using DAS.Web.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DAS.Tests.Functional;

public class SecurityTests(DasWebApplicationFactory factory) : IClassFixture<DasWebApplicationFactory>
{
    [Theory]
    [InlineData("/cuenta/ingresar", null)]
    [InlineData("/inventario", AppRoles.Consulta)]
    [InlineData("/api/tablero", AppRoles.Consulta)]
    public async Task Respuestas_IncluyenEncabezadosDeSeguridad(string url, string? role)
    {
        var response = await factory.CreateClientAs(role).GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csp = response.HeaderValue("Content-Security-Policy");
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("script-src 'self'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.DoesNotContain("unsafe-inline", csp);
        Assert.DoesNotContain("unsafe-eval", csp);
        Assert.Equal("nosniff", response.HeaderValue("X-Content-Type-Options"));
        Assert.Equal("DENY", response.HeaderValue("X-Frame-Options"));
        Assert.Equal("strict-origin-when-cross-origin", response.HeaderValue("Referrer-Policy"));
        Assert.Contains("camera=()", response.HeaderValue("Permissions-Policy"));
    }

    [Theory]
    [InlineData("/inventario")]
    [InlineData("/recetas")]
    [InlineData("/cuenta/ingresar")]
    public async Task Vistas_NoUsanScriptsEstilosNiManejadoresEnLinea(string url)
    {
        var html = await factory.CreateClientAs(AppRoles.Encargado).GetStringAsync(url);

        Assert.DoesNotMatch(new Regex("<script(?![^>]*\\ssrc=)[^>]*>", RegexOptions.IgnoreCase), html);
        Assert.DoesNotMatch(new Regex("\\sstyle=\"", RegexOptions.IgnoreCase), html);
        Assert.DoesNotMatch(new Regex("\\son[a-z]+=\"", RegexOptions.IgnoreCase), html);
    }

    [Fact]
    public async Task CookieAntifalsificacion_EsHttpOnlySameSiteStrictYSegura()
    {
        var response = await factory.CreateClientAs(null).GetAsync("/cuenta/ingresar");

        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AccionesPostDeLasVistas_ExigenTokenAntifalsificacion()
    {
        var actions = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(action => typeof(Controller).IsAssignableFrom(action.ControllerTypeInfo))
            .Where(action => action.ActionConstraints?.OfType<HttpMethodActionConstraint>().Any(constraint => constraint.HttpMethods.Contains("POST")) == true)
            .ToList();

        Assert.NotEmpty(actions);
        Assert.All(actions, action =>
        {
            var closest = action.FilterDescriptors
                .Where(descriptor => AntiforgeryMetadata.DeclaresPolicy(descriptor.Filter))
                .OrderBy(descriptor => descriptor.Scope)
                .LastOrDefault();
            Assert.True(
                closest?.Filter is AutoValidateAntiforgeryTokenAttribute or ValidateAntiForgeryTokenAttribute,
                $"{action.DisplayName} no valida el token antifalsificación");
        });
    }

    [Fact]
    public async Task FormularioSinTokenAntifalsificacion_SeRechazaSinCambiarDatos()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var productId = await factory.ProductIdAsync("Agua mineral");
        var before = await factory.StockOfAsync("Agua mineral");

        var response = await client.PostAsync("/movimientos/registrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Type"] = "Consumo",
            ["ProductId"] = productId.ToString(),
            ["Quantity"] = "1",
            ["Reason"] = "Solicitud falsificada"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await factory.StockOfAsync("Agua mineral"));
    }

    [Fact]
    public async Task ApiDeSesionSinEncabezadoCsrf_SeRechazaAunqueExistaLaCookie()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        await client.CsrfTokenAsync();
        var productId = await factory.ProductIdAsync("Agua mineral");
        var before = await factory.StockOfAsync("Agua mineral");

        var response = await client.PostAsJsonAsync("/api/movimientos", new { productId, type = "Consumo", quantity = 1, reason = "Sin encabezado CSRF" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.MediaType());
        Assert.Equal(before, await factory.StockOfAsync("Agua mineral"));
    }

    [Fact]
    public async Task ApiDeSesionConTokenDeOtraSesion_SeRechaza()
    {
        var attacker = factory.CreateClientAs(AppRoles.Consulta);
        var foreignToken = await attacker.CsrfTokenAsync();
        var victim = factory.CreateClientAs(AppRoles.Encargado);
        await victim.CsrfTokenAsync();
        var productId = await factory.ProductIdAsync("Agua mineral");
        var before = await factory.StockOfAsync("Agua mineral");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/movimientos")
        {
            Content = JsonContent.Create(new { productId, type = "Consumo", quantity = 1, reason = "Token ajeno" })
        };
        request.Headers.Add(SecurityPolicies.AntiforgeryHeaderName, foreignToken);
        var response = await victim.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await factory.StockOfAsync("Agua mineral"));
    }

    [Fact]
    public async Task TextoConEtiquetas_SeCodificaEnHtmlYEnJson()
    {
        var name = $"<script>alert('xss')</script> {Guid.NewGuid():N}";
        await factory.QueryAsync(async context =>
        {
            context.Products.Add(new InventoryProduct
            {
                Name = name,
                Category = "Licores",
                Presentation = "<b>Botella</b>",
                Unit = "botellas",
                BaseUnit = "botellas",
                BaseUnitsPerStockUnit = 1,
                Location = "Barra"
            });
            return await context.SaveChangesAsync();
        });
        var client = factory.CreateClientAs(AppRoles.Consulta);

        var html = await client.GetStringAsync("/inventario");
        var json = await client.GetStringAsync("/api/productos");

        Assert.DoesNotContain("<script>alert(", html);
        Assert.DoesNotContain("<b>Botella</b>", html);
        Assert.Contains("&lt;script&gt;alert(", html);
        Assert.DoesNotContain("<script>", json);
        Assert.Contains("\\u003Cscript\\u003E", json);
    }

    [Fact]
    public async Task FormularioDeProducto_RechazaEtiquetasHtmlEnElServidor()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var token = await DasWebApplicationFactory.AntiforgeryTokenAsync(client, "/productos/nuevo");
        var name = $"<b>Ron</b> {Guid.NewGuid():N}";

        var response = await client.PostAsync("/productos/nuevo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = name,
            ["Category"] = "Licores",
            ["Presentation"] = "Botella 750 ml",
            ["Unit"] = "botellas",
            ["BaseUnit"] = "oz",
            ["BaseUnitsPerStockUnit"] = "25.36",
            ["InitialStock"] = "0",
            ["MinimumStock"] = "1",
            ["UnitCost"] = "10",
            ["Location"] = "Barra"
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no puede contener etiquetas HTML", html);
        Assert.False(await factory.QueryAsync(context => context.Products.AnyAsync(product => product.Name == name)));
    }

    [Fact]
    public async Task SolicitudesDeTokenJwt_SeLimitanPorMinuto()
    {
        await using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting(SecurityPolicies.CredentialAttemptsSetting, "3"));
        var client = limited.CreateHttpsClient();
        var credentials = new { email = "nadie@pruebas.local", password = "Incorrecta#1" };

        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/token", credentials)).StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/auth/token", credentials);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.NotNull(blocked.Headers.RetryAfter);
        Assert.Equal("application/problem+json", blocked.MediaType());
        var problem = await blocked.ReadJsonElementAsync();
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task IntentosDeInicioDeSesion_SeLimitanPorMinuto()
    {
        await using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting(SecurityPolicies.CredentialAttemptsSetting, "2"));
        var client = limited.CreateHttpsClient();
        var token = await DasWebApplicationFactory.AntiforgeryTokenAsync(client, "/cuenta/ingresar");
        FormUrlEncodedContent Attempt() => new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = "nadie@pruebas.local",
            ["Password"] = "Incorrecta#1"
        });

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/cuenta/ingresar", Attempt())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/cuenta/ingresar", Attempt())).StatusCode);
        var blocked = await client.PostAsync("/cuenta/ingresar", Attempt());

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Contains("Demasiados intentos", await blocked.Content.ReadAsStringAsync());
    }
}
