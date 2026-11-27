using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace DAS.Web.OpenApi;

public static class ApiTags
{
    public const string Authentication = "Autenticación (JWT)";
    public const string ProductsJwt = "Productos (JWT)";
    public const string ProductsSession = "Productos (sesión web)";
    public const string Dashboard = "Tablero (sesión web)";
    public const string Movements = "Movimientos (sesión web)";
    public const string PhysicalCounts = "Conteos físicos (sesión web)";
    public const string Recipes = "Recetas (sesión web)";

    public static readonly IReadOnlyList<(string Name, string Description)> Descriptions =
    [
        (Authentication, "Emite el token JWT con correo y contraseña. Copia el access_token en el botón Authorize para probar los endpoints protegidos con Bearer."),
        (ProductsJwt, "CRUD de productos para clientes externos. Requiere token Bearer; crear, editar y desactivar exige el rol Admin o Encargado."),
        (ProductsSession, "Consulta de existencias de solo lectura que usa la cookie de la sesión iniciada en el sitio."),
        (Dashboard, "Indicadores y existencias del tablero; la página de inventario los consulta con Fetch para actualizarse sin recargar."),
        (Movements, "Registro de entradas, consumos y mermas desde el tablero con Fetch. Usa la cookie de sesión y el encabezado X-CSRF-TOKEN."),
        (PhysicalCounts, "Conteo físico rápido de un producto desde el tablero con jQuery AJAX. Usa la cookie de sesión y el encabezado X-CSRF-TOKEN."),
        (Recipes, "Consulta de recetas y registro de preparaciones desde la página de recetas con Fetch. Usa la cookie de sesión y el encabezado X-CSRF-TOKEN.")
    ];

    private static readonly IReadOnlyDictionary<string, string> ByController = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ApiAuth"] = Authentication,
        ["ApiProducts"] = ProductsJwt,
        ["ProductsApi"] = ProductsSession
    };

    public static IList<string> For(ApiDescription description)
    {
        var tags = description.ActionDescriptor.EndpointMetadata.OfType<ITagsMetadata>().LastOrDefault();
        if (tags is not null && tags.Tags.Count > 0)
        {
            return [.. tags.Tags];
        }

        var controller = description.ActionDescriptor.RouteValues.TryGetValue("controller", out var name) ? name ?? string.Empty : string.Empty;
        return [ByController.TryGetValue(controller, out var tag) ? tag : controller];
    }
}
