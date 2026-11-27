using System.Net;
using System.Text.RegularExpressions;
using DAS.Application.Security;

namespace DAS.Tests.Functional;

public class ValidationTests(DasWebApplicationFactory factory) : IClassFixture<DasWebApplicationFactory>
{
    [Fact]
    public async Task Login_IncluyeValidacionDelLadoDelClienteEnEspanol()
    {
        var html = await factory.CreateClientAs(null).GetStringAsync("/cuenta/ingresar");

        Assert.Contains("data-val-required=\"Escribe tu correo electrónico.\"", html);
        Assert.Contains("data-val-email=\"Escribe un correo electrónico válido.\"", html);
        Assert.Contains("data-val-required=\"Escribe tu contraseña.\"", html);
        Assert.Contains("jquery.validate.unobtrusive.min.js", html);
        Assert.Contains("/js/validation.js", html);
    }

    [Fact]
    public async Task LoginVacio_SeValidaTambienEnElServidor()
    {
        var client = factory.CreateClientAs(null);
        var token = await DasWebApplicationFactory.AntiforgeryTokenAsync(client, "/cuenta/ingresar");

        var response = await client.PostAsync("/cuenta/ingresar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = string.Empty,
            ["Password"] = string.Empty
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches("field-validation-error\"[^>]*data-valmsg-for=\"Email\"[^>]*>Escribe tu correo electrónico.<", html);
        Assert.Matches("field-validation-error\"[^>]*data-valmsg-for=\"Password\"[^>]*>Escribe tu contraseña.<", html);
    }

    [Fact]
    public async Task ModalDeMovimiento_GeneraLasReglasDelViewModelParaElCliente()
    {
        var html = await factory.CreateClientAs(AppRoles.Encargado).GetStringAsync("/inventario");

        var quantity = Regex.Match(html, "<input[^>]*name=\"Quantity\"[^>]*>").Value;
        Assert.Contains("data-val-required=\"Indica la cantidad.\"", quantity);
        Assert.Contains("data-val-range=\"La cantidad debe estar entre 0.01 y 1000000.\"", quantity);
        var reason = Regex.Match(html, "<input[^>]*name=\"Reason\"[^>]*>").Value;
        Assert.Contains("data-val-plaintext=\"Motivo no puede contener etiquetas HTML ni los caracteres &lt; o &gt;.\"", reason);
        Assert.Contains("data-val-length-max=\"120\"", reason);
        var product = Regex.Match(html, "<select[^>]*name=\"ProductId\"[^>]*>").Value;
        Assert.Contains("data-val-required=\"Selecciona un producto.\"", product);
    }

    [Fact]
    public async Task FormularioDeProducto_IncluyeElAdaptadorDeTextoPlano()
    {
        var html = await factory.CreateClientAs(AppRoles.Encargado).GetStringAsync("/productos/nuevo");

        Assert.Contains("data-val-plaintext=\"Nombre del producto no puede contener etiquetas HTML", html);
        Assert.Contains("data-val-remote-url=", html);
        Assert.Contains("/js/validation.js", html);
    }

    [Fact]
    public async Task ApiDeSesion_DevuelveProblemDetailsConErroresPorCampo()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);

        var response = await client.PostJsonWithCsrfAsync("/api/movimientos", new { productId = 0, type = "Consumo", quantity = 0, reason = "<b>x</b>" });
        var problem = await response.ReadJsonElementAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.MediaType());
        Assert.Equal("Los datos enviados no son válidos.", problem.GetProperty("title").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.Contains("Selecciona un producto.", problem.ErrorsFor("ProductId"));
        Assert.Contains("La cantidad debe estar entre 0.01 y 1000000.", problem.ErrorsFor("Quantity"));
        Assert.Contains(problem.ErrorsFor("Reason"), message => message.Contains("etiquetas HTML"));
    }

    [Fact]
    public async Task ApiDeSesion_AplicaLasReglasDelViewModelYDelServicio()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var productId = await factory.ProductIdAsync("Agua mineral");

        var adjustment = await client.PostJsonWithCsrfAsync("/api/movimientos", new { productId, type = "Ajuste", quantity = 1, reason = "Ajuste manual" });
        var inactive = await client.PostJsonWithCsrfAsync("/api/movimientos", new { productId = 999999, type = "Entrada", quantity = 1, reason = "Producto inexistente" });

        Assert.Equal(HttpStatusCode.BadRequest, adjustment.StatusCode);
        Assert.Contains("Los ajustes solo se generan desde el conteo físico.", (await adjustment.ReadJsonElementAsync()).ErrorsFor("Type"));
        Assert.Equal(HttpStatusCode.BadRequest, inactive.StatusCode);
        Assert.Contains("Selecciona un producto activo.", (await inactive.ReadJsonElementAsync()).ErrorsFor("ProductId"));
    }

    [Fact]
    public async Task ApiDeSesion_JsonMalFormado_Responde400()
    {
        var client = factory.CreateClientAs(AppRoles.Encargado);
        var token = await client.CsrfTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/movimientos")
        {
            Content = new StringContent("{\"productId\": ", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add(DAS.Web.Security.SecurityPolicies.AntiforgeryHeaderName, token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.MediaType());
    }
}
