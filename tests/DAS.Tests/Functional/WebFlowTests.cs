using System.Net;
using System.Text;
using DAS.Application.Security;
using DAS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DAS.Tests.Functional;

//Recorre la aplicación por HTTP: rutas, autorización por rol, antifalsificación, servicios y base de datos
public class WebFlowTests(DasWebApplicationFactory factory) : IClassFixture<DasWebApplicationFactory>
{
    [Fact]
    public async Task SinSesion_RedirigeAlInicioDeSesion()
    {
        var response = await factory.CreateClientAs(null).GetAsync("/inventario");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/cuenta/ingresar", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task InicioDeSesion_EsUnaPaginaPublica()
    {
        var response = await factory.CreateClientAs(null).GetAsync("/cuenta/ingresar");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Tablero_MuestraLosProductosGuardadosEnLaBaseDeDatos()
    {
        var html = await factory.CreateClientAs(AppRoles.Consulta).GetStringAsync("/inventario");

        Assert.Contains("Cerveza Pilsener", html);
        Assert.Contains("Tequila blanco", html);
        Assert.DoesNotContain("+ Registrar movimiento", html);
    }

    [Fact]
    public async Task RegistrarConsumo_ActualizaLaExistenciaYGuardaQuienLoHizo()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var token = await DasWebApplicationFactory.AntiforgeryTokenAsync(client, "/inventario");
        var productId = await factory.ProductIdAsync("Agua mineral");
        var before = await factory.StockOfAsync("Agua mineral");

        var response = await client.PostAsync("/movimientos/registrar", Form(token,
            ("Type", nameof(MovementType.Consumo)), ("ProductId", productId.ToString()), ("Quantity", "1"), ("Reason", "Prueba funcional")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(before - 1, await factory.StockOfAsync("Agua mineral"));
        var last = await factory.QueryAsync(context => context.Movements.AsNoTracking()
            .Where(movement => movement.ProductId == productId)
            .OrderByDescending(movement => movement.Id)
            .FirstAsync());
        Assert.Equal("Prueba funcional", last.Reason);
        Assert.Equal(TestAuthHandler.UserNameFor(AppRoles.Encargado), last.UserName);
    }

    [Fact]
    public async Task ConsumoMayorQueLaExistencia_MuestraElErrorYNoCambiaElSaldo()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var token = await DasWebApplicationFactory.AntiforgeryTokenAsync(client, "/inventario");
        var productId = await factory.ProductIdAsync("Tequila blanco");
        var before = await factory.StockOfAsync("Tequila blanco");

        await client.PostAsync("/movimientos/registrar", Form(token,
            ("Type", nameof(MovementType.Consumo)), ("ProductId", productId.ToString()), ("Quantity", "9999"), ("Reason", "Prueba de saldo")));
        var html = await client.GetStringAsync("/inventario");

        Assert.Contains("La existencia disponible de Tequila blanco", html);
        Assert.Equal(before, await factory.StockOfAsync("Tequila blanco"));
    }

    [Fact]
    public async Task RolConsulta_NoPuedeRegistrarMovimientos()
    {
        var client = factory.CreateClientAs(AppRoles.Consulta);
        var token = await DasWebApplicationFactory.AntiforgeryTokenAsync(client, "/inventario");
        var productId = await factory.ProductIdAsync("Vasos acrílicos");
        var before = await factory.StockOfAsync("Vasos acrílicos");

        var response = await client.PostAsync("/movimientos/registrar", Form(token,
            ("Type", nameof(MovementType.Consumo)), ("ProductId", productId.ToString()), ("Quantity", "1"), ("Reason", "Sin permiso")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/cuenta/acceso-denegado", response.Headers.Location!.OriginalString);
        Assert.Equal(before, await factory.StockOfAsync("Vasos acrílicos"));
    }

    [Fact]
    public async Task PrepararReceta_DescuentaTodosLosIngredientes()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var token = await DasWebApplicationFactory.AntiforgeryTokenAsync(client, "/recetas");
        var recipeId = await factory.QueryAsync(async context => (await context.Recipes.SingleAsync(recipe => recipe.Name == "Michelada")).Id);
        var beerBefore = await factory.StockOfAsync("Cerveza Pilsener");
        var limeBefore = await factory.StockOfAsync("Limón");

        var response = await client.PostAsync($"/recetas/{recipeId}/preparar", Form(token, ("Servings", "2")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(beerBefore - 2, await factory.StockOfAsync("Cerveza Pilsener"));
        Assert.Equal(limeBefore - 2, await factory.StockOfAsync("Limón"));
    }

    [Fact]
    public async Task ConteoRapido_RegistraElAjustePorLaDiferencia()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var token = await DasWebApplicationFactory.AntiforgeryTokenAsync(client, "/inventario");
        var productId = await factory.ProductIdAsync("Pajillas");
        var before = await factory.StockOfAsync("Pajillas");

        var response = await client.PostAsync("/inventario/conteo/producto", Form(token,
            ("ProductId", productId.ToString()), ("ActualStock", (before - 1).ToString(System.Globalization.CultureInfo.InvariantCulture))));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(before - 1, await factory.StockOfAsync("Pajillas"));
        Assert.True(await factory.QueryAsync(context => context.Movements.AnyAsync(movement => movement.ProductId == productId && movement.Type == MovementType.Ajuste)));
    }

    [Fact]
    public async Task ReporteMensual_SeExportaComoCsv()
    {
        var response = await factory.CreateClientAs(AppRoles.Consulta).GetAsync("/reportes/mensual/exportar");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("Producto,Categoría", Encoding.UTF8.GetString(bytes[3..]));
    }

    [Fact]
    public async Task FormatoDeExportacionDesconocido_Responde404()
    {
        var response = await factory.CreateClientAs(AppRoles.Consulta).GetAsync("/reportes/mensual/exportar?formato=xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Encargado, HttpStatusCode.Redirect)]
    [InlineData(AppRoles.Admin, HttpStatusCode.OK)]
    public async Task AdministracionDeUsuarios_SoloParaAdministradores(string role, HttpStatusCode expected)
    {
        var response = await factory.CreateClientAs(role).GetAsync("/admin/usuarios");

        Assert.Equal(expected, response.StatusCode);
    }

    private static FormUrlEncodedContent Form(string token, params (string Name, string Value)[] fields) =>
        new([new KeyValuePair<string, string>("__RequestVerificationToken", token), .. fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value))]);
}
