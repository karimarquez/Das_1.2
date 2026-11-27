using System.Net;
using System.Text.Json;
using DAS.Web.Extensions;
using DAS.Web.Security;
using Microsoft.AspNetCore.Hosting;

namespace DAS.Tests.Functional;

public class ApiDocumentationTests(DasWebApplicationFactory factory) : IClassFixture<DasWebApplicationFactory>
{
    [Fact]
    public async Task DocumentoOpenApi_EsPublicoYDescribeLaApi()
    {
        var response = await factory.CreateClientAs(null).GetAsync("/swagger/v1/swagger.json");
        var document = await response.ReadJsonElementAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.MediaType());
        Assert.StartsWith("3.", document.GetProperty("openapi").GetString());
        Assert.Equal("Pipiris Nais · API de inventario", document.GetProperty("info").GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("/api/auth/token", "post")]
    [InlineData("/api/inventario/productos", "get")]
    [InlineData("/api/inventario/productos", "post")]
    [InlineData("/api/inventario/productos/{id}", "get")]
    [InlineData("/api/inventario/productos/{id}", "put")]
    [InlineData("/api/inventario/productos/{id}", "delete")]
    [InlineData("/api/productos", "get")]
    [InlineData("/api/productos/{id}", "get")]
    [InlineData("/api/tablero", "get")]
    [InlineData("/api/movimientos", "post")]
    [InlineData("/api/conteos", "post")]
    [InlineData("/api/conteos/{id}", "get")]
    [InlineData("/api/recetas", "get")]
    [InlineData("/api/recetas/{id}/preparaciones", "post")]
    public async Task CadaEndpoint_TieneResumenEtiquetaYRespuestas(string path, string method)
    {
        var operation = (await DocumentAsync()).GetProperty("paths").GetProperty(path).GetProperty(method);

        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.NotEmpty(operation.GetProperty("tags").EnumerateArray());
        Assert.NotEmpty(operation.GetProperty("responses").EnumerateObject());
    }

    [Fact]
    public async Task SoloSeDocumentanLasRutasDeLaApi()
    {
        var paths = (await DocumentAsync()).GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();

        Assert.All(paths, path => Assert.StartsWith("/api/", path));
    }

    [Fact]
    public async Task EsquemasDeSeguridad_DistinguenJwtYSesionWeb()
    {
        var document = await DocumentAsync();
        var schemes = document.GetProperty("components").GetProperty("securitySchemes");
        var bearer = schemes.GetProperty("Bearer");

        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
        Assert.Equal("cookie", schemes.GetProperty("SesionWeb").GetProperty("in").GetString());
        Assert.Equal(new[] { "Bearer" }, SecurityOf(document, "/api/inventario/productos", "post"));
        Assert.Equal(new[] { "SesionWeb" }, SecurityOf(document, "/api/movimientos", "post"));
        Assert.Empty(SecurityOf(document, "/api/auth/token", "post"));
    }

    [Theory]
    [InlineData("/api/movimientos")]
    [InlineData("/api/conteos")]
    [InlineData("/api/recetas/{id}/preparaciones")]
    public async Task OperacionesDeSesionQueModifican_DocumentanElEncabezadoCsrf(string path)
    {
        var operation = (await DocumentAsync()).GetProperty("paths").GetProperty(path).GetProperty("post");

        var header = Assert.Single(operation.GetProperty("parameters").EnumerateArray(), parameter =>
            parameter.GetProperty("name").GetString() == SecurityPolicies.AntiforgeryHeaderName);
        Assert.Equal("header", header.GetProperty("in").GetString());
        Assert.True(header.GetProperty("required").GetBoolean());
    }

    [Fact]
    public async Task OperacionesJwt_NoPidenEncabezadoCsrf()
    {
        var document = await DocumentAsync();

        foreach (var method in new[] { "post", "put", "delete" })
        {
            var path = method == "post" ? "/api/inventario/productos" : "/api/inventario/productos/{id}";
            var operation = document.GetProperty("paths").GetProperty(path).GetProperty(method);
            var headers = operation.TryGetProperty("parameters", out var parameters)
                ? parameters.EnumerateArray().Where(parameter => parameter.GetProperty("in").GetString() == "header").ToList()
                : [];
            Assert.Empty(headers);
        }
    }

    [Fact]
    public async Task ComentariosXmlYAtributos_SeIncluyenEnLaDocumentacion()
    {
        var document = await DocumentAsync();
        var create = document.GetProperty("paths").GetProperty("/api/inventario/productos").GetProperty("post");
        var movement = document.GetProperty("paths").GetProperty("/api/movimientos").GetProperty("post");

        Assert.Equal("Crea un producto nuevo.", create.GetProperty("summary").GetString());
        Assert.Equal("Producto creado.", create.GetProperty("responses").GetProperty("201").GetProperty("description").GetString());
        Assert.Contains("403", create.GetProperty("responses").EnumerateObject().Select(response => response.Name));
        Assert.Equal("Registra una entrada, consumo o merma", movement.GetProperty("summary").GetString());
        Assert.Contains("transacción", movement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Esquemas_ReflejanLasValidacionesDelServidor()
    {
        var schemas = (await DocumentAsync()).GetProperty("components").GetProperty("schemas");
        var product = schemas.GetProperty("SaveProductDto");
        var name = product.GetProperty("properties").GetProperty("name");
        var movementTypes = schemas.GetProperty("MovementType").GetProperty("enum").EnumerateArray().Select(value => value.GetString());

        Assert.Equal(80, name.GetProperty("maxLength").GetInt32());
        Assert.Equal(2, name.GetProperty("minLength").GetInt32());
        Assert.Contains("name", product.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "Entrada", "Consumo", "Merma", "Ajuste" }, movementTypes);
        Assert.True(schemas.TryGetProperty("ValidationProblemDetails", out _));
    }

    [Fact]
    public async Task EtiquetasDelDocumento_TienenDescripcion()
    {
        var tags = (await DocumentAsync()).GetProperty("tags").EnumerateArray().ToList();

        Assert.Equal(7, tags.Count);
        Assert.All(tags, tag => Assert.False(string.IsNullOrWhiteSpace(tag.GetProperty("description").GetString())));
    }

    [Fact]
    public async Task InterfazSwagger_SeSirveConUnaPoliticaDeContenidoPropia()
    {
        var response = await factory.CreateClientAs(null).GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("swagger-ui", await response.Content.ReadAsStringAsync());
        Assert.Contains("script-src 'self'", response.HeaderValue("Content-Security-Policy"));
        Assert.Contains("frame-ancestors 'none'", response.HeaderValue("Content-Security-Policy"));
    }

    [Fact]
    public async Task Documentacion_SePuedeDesactivarPorConfiguracion()
    {
        await using var disabled = factory.WithWebHostBuilder(builder => builder.UseSetting(ApiDocumentationExtensions.EnabledSetting, "false"));

        var response = await disabled.CreateHttpsClient().GetAsync("/swagger/v1/swagger.json");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<JsonElement> DocumentAsync() =>
        await (await factory.CreateClientAs(null).GetAsync("/swagger/v1/swagger.json")).ReadJsonElementAsync();

    private static IReadOnlyList<string> SecurityOf(JsonElement document, string path, string method)
    {
        var operation = document.GetProperty("paths").GetProperty(path).GetProperty(method);
        return operation.TryGetProperty("security", out var security)
            ? security.EnumerateArray().SelectMany(requirement => requirement.EnumerateObject().Select(scheme => scheme.Name)).ToList()
            : [];
    }
}
