using System.Net;
using System.Net.Http.Json;
using DAS.Application.DTOs;
using DAS.Application.Security;
using DAS.Domain.Enums;
using DAS.Web.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace DAS.Tests.Functional;

public class AsyncEndpointsTests(DasWebApplicationFactory factory) : IClassFixture<DasWebApplicationFactory>
{
    [Fact]
    public async Task Tablero_DevuelveIndicadoresYExistenciasEnJson()
    {
        var response = await factory.CreateClientAs(AppRoles.Consulta).GetAsync("/api/tablero");
        var body = await response.Content.ReadAsStringAsync();
        var dashboard = await response.ReadJsonAsync<DashboardResponse>();
        var stored = await factory.QueryAsync(context => context.Products.AsNoTracking().Where(product => product.IsActive).ToListAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.MediaType());
        Assert.Contains("no-store", response.HeaderValue("Cache-Control"));
        Assert.Equal(stored.Count, dashboard.ProductCount);
        Assert.Equal(stored.Count(product => product.Stock <= product.MinimumStock), dashboard.LowStockCount);
        Assert.Equal(stored.Select(product => (product.Id, product.Stock)).OrderBy(item => item.Id), dashboard.Products.Select(product => (product.Id, product.Stock)).OrderBy(item => item.Id));
        Assert.DoesNotContain("unitCost", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarMovimiento_ActualizaLaExistenciaYDevuelveElNuevoSaldo()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var productId = await factory.ProductIdAsync("Agua mineral");
        var before = await factory.StockOfAsync("Agua mineral");

        var response = await client.PostJsonWithCsrfAsync("/api/movimientos", new { productId, type = "Consumo", quantity = 1.5m, reason = "Registro desde Fetch" });
        var result = await response.ReadJsonAsync<MovementRegisteredResponse>();
        var after = await factory.StockOfAsync("Agua mineral");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.MediaType());
        Assert.Equal(MovementType.Consumo, result.Movement.Type);
        Assert.Equal(before - 1.5m, after);
        Assert.Equal(after, result.Product.Stock);
        Assert.Equal(after, result.Movement.StockAfter);
        Assert.Equal(before, result.Movement.StockBefore);
        Assert.Equal(TestAuthHandler.UserNameFor(AppRoles.Encargado), result.Movement.UserName);
        Assert.True(await factory.QueryAsync(context => context.Movements.AnyAsync(movement => movement.Id == result.Movement.Id && movement.Reason == "Registro desde Fetch")));
    }

    [Fact]
    public async Task MovimientoMayorQueLaExistencia_Responde400ConElCampoYNoCambiaElSaldo()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var productId = await factory.ProductIdAsync("Tequila blanco");
        var before = await factory.StockOfAsync("Tequila blanco");

        var response = await client.PostJsonWithCsrfAsync("/api/movimientos", new { productId, type = "Merma", quantity = 9999, reason = "Saldo insuficiente" });
        var problem = await response.ReadJsonElementAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(problem.ErrorsFor("Quantity"), message => message.StartsWith("La existencia disponible de Tequila blanco"));
        Assert.Equal(before, await factory.StockOfAsync("Tequila blanco"));
    }

    [Fact]
    public async Task RolConsulta_NoPuedeRegistrarPorLaApi()
    {
        var client = factory.CreateClientAs(AppRoles.Consulta);
        var productId = await factory.ProductIdAsync("Vasos acrílicos");
        var before = await factory.StockOfAsync("Vasos acrílicos");

        var response = await client.PostJsonWithCsrfAsync("/api/movimientos", new { productId, type = "Consumo", quantity = 1, reason = "Sin permiso" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.MediaType());
        Assert.Equal("No tienes permiso para realizar esta acción.", (await response.ReadJsonElementAsync()).GetProperty("title").GetString());
        Assert.Equal(before, await factory.StockOfAsync("Vasos acrílicos"));
    }

    [Theory]
    [InlineData("GET", "/api/tablero")]
    [InlineData("GET", "/api/productos")]
    [InlineData("POST", "/api/movimientos")]
    public async Task SinSesion_LaApiResponde401EnJsonEnLugarDeRedirigir(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { productId = 1, type = "Entrada", quantity = 1, reason = "Sin sesión" });
        }

        var response = await factory.CreateClientAs(null).SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.MediaType());
        Assert.Equal("Inicia sesión para continuar.", (await response.ReadJsonElementAsync()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task ConteoRapido_RegistraElAjusteYPublicaLaUbicacionDelResultado()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var productId = await factory.ProductIdAsync("Pajillas");
        var before = await factory.StockOfAsync("Pajillas");

        var response = await client.PostJsonWithCsrfAsync("/api/conteos", new { productId, actualStock = before + 2 });
        var result = await response.ReadJsonAsync<PhysicalCountRegisteredResponse>();
        var line = Assert.Single(result.Count.Lines);
        var detail = await client.GetAsync(response.Headers.Location);
        var stored = await detail.ReadJsonAsync<PhysicalCountDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/conteos/{result.Count.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(before, line.TheoreticalStock);
        Assert.Equal(2, line.Difference);
        Assert.Equal(before + 2, result.Product.Stock);
        Assert.Equal(before + 2, await factory.StockOfAsync("Pajillas"));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(result.Count.Id, stored.Id);
        Assert.True(await factory.QueryAsync(context => context.Movements.AnyAsync(movement =>
            movement.ProductId == productId && movement.Type == MovementType.Ajuste && movement.Reason.Contains($"#{result.Count.Id}"))));
    }

    [Fact]
    public async Task ConteoDeProductoInexistente_Responde400SinCrearConteo()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var countsBefore = await factory.QueryAsync(context => context.PhysicalCounts.CountAsync());

        var response = await client.PostJsonWithCsrfAsync("/api/conteos", new { productId = 999999, actualStock = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(countsBefore, await factory.QueryAsync(context => context.PhysicalCounts.CountAsync()));
    }

    [Fact]
    public async Task PrepararReceta_DescuentaTodosLosIngredientesYLosDetalla()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var recipeId = await factory.QueryAsync(async context => (await context.Recipes.SingleAsync(recipe => recipe.Name == "Michelada")).Id);
        var beerBefore = await factory.StockOfAsync("Cerveza Pilsener");
        var limeBefore = await factory.StockOfAsync("Limón");

        var response = await client.PostJsonWithCsrfAsync($"/api/recetas/{recipeId}/preparaciones", new { servings = 2 });
        var preparation = await response.ReadJsonAsync<RecipePreparationDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Michelada", preparation.RecipeName);
        Assert.Equal(2, preparation.Servings);
        Assert.Contains(preparation.Deductions, item => item.ProductName == "Cerveza Pilsener" && item.Quantity == 2);
        Assert.Contains(preparation.Deductions, item => item.ProductName == "Limón" && item.Quantity == 2);
        Assert.Equal(beerBefore - 2, await factory.StockOfAsync("Cerveza Pilsener"));
        Assert.Equal(limeBefore - 2, await factory.StockOfAsync("Limón"));
    }

    [Fact]
    public async Task PrepararRecetaSinExistenciaSuficiente_NoDescuentaNingunIngrediente()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var recipeId = await factory.QueryAsync(async context => (await context.Recipes.SingleAsync(recipe => recipe.Name == "Margarita clásica")).Id);
        var tequilaBefore = await factory.StockOfAsync("Tequila blanco");
        var limeBefore = await factory.StockOfAsync("Limón");
        var movementsBefore = await factory.QueryAsync(context => context.Movements.CountAsync());

        var response = await client.PostJsonWithCsrfAsync($"/api/recetas/{recipeId}/preparaciones", new { servings = 1000 });
        var problem = await response.ReadJsonElementAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(problem.ErrorsFor(string.Empty), message => message.StartsWith("Existencia insuficiente de"));
        Assert.Equal(tequilaBefore, await factory.StockOfAsync("Tequila blanco"));
        Assert.Equal(limeBefore, await factory.StockOfAsync("Limón"));
        Assert.Equal(movementsBefore, await factory.QueryAsync(context => context.Movements.CountAsync()));
    }

    [Fact]
    public async Task PrepararRecetaInexistente_Responde404()
    {
        var response = await factory.CreateClientAs(AppRoles.Encargado).PostJsonWithCsrfAsync("/api/recetas/999999/preparaciones", new { servings = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.MediaType());
    }

    [Fact]
    public async Task Recetas_SeConsultanEnJson()
    {
        var response = await factory.CreateClientAs(AppRoles.Consulta).GetAsync("/api/recetas");
        var recipes = await response.ReadJsonAsync<List<RecipeDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var michelada = Assert.Single(recipes, recipe => recipe.Name == "Michelada");
        Assert.Equal(2, michelada.Ingredients.Count);
    }

    [Fact]
    public async Task ActividadReciente_DevuelveSoloLaVistaParcial()
    {
        var response = await factory.CreateClientAs(AppRoles.Consulta).GetAsync("/inventario/actividad");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.MediaType());
        Assert.Contains("class=\"activity-list\"", html);
        Assert.DoesNotContain("<html", html);
        Assert.DoesNotContain("sidebar", html);
    }

    [Fact]
    public async Task Tablero_IncluyeLosEnlacesQueUsaElCliente()
    {
        var html = await factory.CreateClientAs(AppRoles.Encargado).GetStringAsync("/inventario");

        Assert.Contains("data-dashboard-url=\"/api/tablero\"", html);
        Assert.Contains("data-activity-url=\"/inventario/actividad\"", html);
        Assert.Contains("data-async-url=\"/api/movimientos\"", html);
        Assert.Contains("data-async-url=\"/api/conteos\"", html);
        Assert.Contains("/js/inventory.js", html);
    }
}
