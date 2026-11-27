using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DAS.Application.DTOs;
using DAS.Application.Security;
using DAS.Domain.Enums;
using DAS.Web.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace DAS.Tests.Functional;

public class ProductApiTests(DasWebApplicationFactory factory) : IClassFixture<DasWebApplicationFactory>
{
    private const string ProductsUrl = "/api/inventario/productos";

    [Fact]
    public async Task CicloCompletoConJwt_CreaConsultaActualizaYDesactiva()
    {
        var client = await factory.CreateJwtClientAsync(AppRoles.Encargado);
        var name = UniqueName("Ginebra");

        var created = await client.PostAsJsonAsync(ProductsUrl, NewProduct(name, initialStock: 6));
        var product = await created.ReadJsonAsync<ProductDto>();
        var fetched = await (await client.GetAsync(created.Headers.Location)).ReadJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal($"{ProductsUrl}/{product.Id}", created.Headers.Location!.AbsolutePath);
        Assert.Equal(name, fetched.Name);
        Assert.Equal(6, fetched.Stock);
        Assert.True(fetched.IsActive);

        var updated = await client.PutAsJsonAsync($"{ProductsUrl}/{product.Id}", NewProduct($"{name} reserva", initialStock: 99) with { MinimumStock = 8 });
        var afterUpdate = await updated.ReadJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal($"{name} reserva", afterUpdate.Name);
        Assert.Equal(8, afterUpdate.MinimumStock);
        Assert.Equal(6, afterUpdate.Stock);
        Assert.True(afterUpdate.IsLowStock);

        var deleted = await client.DeleteAsync($"{ProductsUrl}/{product.Id}");
        var afterDelete = await (await client.GetAsync($"{ProductsUrl}/{product.Id}")).ReadJsonAsync<ProductDto>();
        var search = await (await client.GetAsync($"{ProductsUrl}?search={Uri.EscapeDataString(name)}")).ReadJsonAsync<List<ProductStockDto>>();

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(afterDelete.IsActive);
        Assert.Empty(search);
        Assert.True(await factory.QueryAsync(context => context.Movements.AnyAsync(movement => movement.ProductId == product.Id)));
    }

    [Fact]
    public async Task CrearProductoInvalido_Responde400ConErroresPorCampo()
    {
        var client = await factory.CreateJwtClientAsync(AppRoles.Encargado);

        var response = await client.PostAsJsonAsync(ProductsUrl, NewProduct(string.Empty) with { UnitCost = -1, Location = "<i>Barra</i>" });
        var problem = await response.ReadJsonElementAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.MediaType());
        Assert.Equal("Los datos enviados no son válidos.", problem.GetProperty("title").GetString());
        Assert.Contains("Escribe el nombre del producto.", problem.ErrorsFor("Name"));
        Assert.Contains("El costo debe estar entre 0 y 100000.", problem.ErrorsFor("UnitCost"));
        Assert.Contains(problem.ErrorsFor("Location"), message => message.Contains("etiquetas HTML"));
    }

    [Fact]
    public async Task CrearProductoFueraDelCatalogo_Responde400()
    {
        var client = await factory.CreateJwtClientAsync(AppRoles.Encargado);

        var response = await client.PostAsJsonAsync(ProductsUrl, NewProduct(UniqueName("Vino")) with { Category = "Electrónica", Unit = "oz", BaseUnit = "oz", BaseUnitsPerStockUnit = 2 });
        var problem = await response.ReadJsonElementAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("La categoría no pertenece al catálogo.", problem.ErrorsFor("Category"));
        Assert.Contains("La unidad de control no pertenece al catálogo.", problem.ErrorsFor("Unit"));
        Assert.Contains("Si la unidad de control y la unidad base son iguales, la equivalencia debe ser 1.", problem.ErrorsFor("BaseUnitsPerStockUnit"));
    }

    [Fact]
    public async Task ApiJwt_NoDependeDelTokenAntifalsificacion()
    {
        var client = await factory.CreateJwtClientAsync(AppRoles.Admin);

        var response = await client.PostAsJsonAsync(ProductsUrl, NewProduct(UniqueName("Whisky")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RolConsultaConJwt_PuedeConsultarPeroNoModificar()
    {
        var client = await factory.CreateJwtClientAsync(AppRoles.Consulta);

        var list = await client.GetAsync(ProductsUrl);
        var create = await client.PostAsJsonAsync(ProductsUrl, NewProduct(UniqueName("Vermut")));

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task ProductoInexistente_Responde404()
    {
        var client = await factory.CreateJwtClientAsync(AppRoles.Encargado);

        var get = await client.GetAsync($"{ProductsUrl}/999999");
        var put = await client.PutAsJsonAsync($"{ProductsUrl}/999999", NewProduct(UniqueName("Cognac")));
        var delete = await client.DeleteAsync($"{ProductsUrl}/999999");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task TokenAlterado_Responde401ConDesafioBearer()
    {
        var client = await factory.CreateJwtClientAsync(AppRoles.Encargado);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token[..^4] + (token.EndsWith("AAAA", StringComparison.Ordinal) ? "BBBB" : "AAAA"));

        var response = await client.GetAsync(ProductsUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal("application/problem+json", response.MediaType());
    }

    [Fact]
    public async Task TokenRequest_ValidaElCorreoAntesDeAutenticar()
    {
        var response = await factory.CreateClientAs(null).PostAsJsonAsync("/api/auth/token", new { email = "no-es-correo", password = string.Empty });
        var problem = await response.ReadJsonElementAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Escribe un correo electrónico válido.", problem.ErrorsFor("Email"));
        Assert.Contains("Escribe la contraseña.", problem.ErrorsFor("Password"));
    }

    [Fact]
    public async Task RespuestasJson_CumplenElContratoDocumentado()
    {
        var client = await factory.CreateJwtClientAsync(AppRoles.Consulta);

        var response = await client.GetAsync($"{ProductsUrl}?search=cerveza&page=1&pageSize=5");
        var products = await response.ReadJsonElementAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.MediaType());
        Assert.Equal("utf-8", response.Content.Headers.ContentType!.CharSet);
        Assert.Equal(JsonValueKind.Array, products.ValueKind);
        var first = products.EnumerateArray().First();
        Assert.Equal(
            new[] { "id", "name", "category", "stock", "unit", "minimumStock", "isLowStock" },
            first.EnumerateObject().Select(property => property.Name));
        Assert.Equal(JsonValueKind.Number, first.GetProperty("stock").ValueKind);
        Assert.Contains(first.GetProperty("isLowStock").ValueKind, new[] { JsonValueKind.True, JsonValueKind.False });
    }

    [Fact]
    public async Task Existencia_EsConsistenteEntreApisBaseDeDatosYKardex()
    {
        var jwt = await factory.CreateJwtClientAsync(AppRoles.Encargado);
        var web = factory.CreateClientAs(AppRoles.Encargado);
        var created = await (await jwt.PostAsJsonAsync(ProductsUrl, NewProduct(UniqueName("Ron blanco"), initialStock: 10))).ReadJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.Created, (await web.PostJsonWithCsrfAsync("/api/movimientos", Movement(created.Id, "Consumo", 3))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await web.PostJsonWithCsrfAsync("/api/movimientos", Movement(created.Id, "Entrada", 5))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await web.PostJsonWithCsrfAsync("/api/movimientos", Movement(created.Id, "Merma", 1.5m))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await web.PostJsonWithCsrfAsync("/api/conteos", new { productId = created.Id, actualStock = 10 })).StatusCode);

        var viaJwt = await (await jwt.GetAsync($"{ProductsUrl}/{created.Id}")).ReadJsonAsync<ProductDto>();
        var viaSession = await (await web.GetAsync($"/api/productos/{created.Id}")).ReadJsonAsync<ProductStockDto>();
        var viaDashboard = (await (await web.GetAsync("/api/tablero")).ReadJsonAsync<DashboardResponse>()).Products.Single(product => product.Id == created.Id);
        var stored = await factory.QueryAsync(context => context.Products.AsNoTracking().SingleAsync(product => product.Id == created.Id));
        var kardex = await factory.QueryAsync(context => context.Movements.AsNoTracking()
            .Where(movement => movement.ProductId == created.Id)
            .OrderBy(movement => movement.Id)
            .ToListAsync());

        Assert.Equal(10m, stored.Stock);
        Assert.Equal(stored.Stock, viaJwt.Stock);
        Assert.Equal(stored.Stock, viaSession.Stock);
        Assert.Equal(stored.Stock, viaDashboard.Stock);
        Assert.Equal(
            new[] { MovementType.Entrada, MovementType.Consumo, MovementType.Entrada, MovementType.Merma, MovementType.Ajuste },
            kardex.Select(movement => movement.Type));
        Assert.Equal(0, kardex[0].StockBefore);
        Assert.All(kardex.Skip(1).Zip(kardex), pair => Assert.Equal(pair.Second.StockAfter, pair.First.StockBefore));
        Assert.Equal(stored.Stock, kardex[^1].StockAfter);
        Assert.Equal(stored.Stock, kardex.Sum(movement => movement.StockAfter - movement.StockBefore));
    }

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    private static object Movement(int productId, string type, decimal quantity) =>
        new { productId, type, quantity, reason = $"Prueba de consistencia: {type}" };

    private static SaveProductDto NewProduct(string name, decimal initialStock = 0) => new()
    {
        Name = name,
        Category = "Licores",
        Presentation = "Botella 750 ml",
        Unit = "botellas",
        BaseUnit = "oz",
        BaseUnitsPerStockUnit = 25.36m,
        MinimumStock = 2,
        UnitCost = 12.5m,
        Location = "Estante de licores",
        InitialStock = initialStock
    };
}
