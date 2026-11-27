using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DAS.Web.OpenApi;

public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        var authorization = metadata.OfType<IAuthorizeData>().ToList();
        var usesBearer = authorization
            .SelectMany(data => (data.AuthenticationSchemes ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Contains(JwtBearerDefaults.AuthenticationScheme, StringComparer.Ordinal);
        var scheme = usesBearer ? OpenApiSecuritySchemes.Bearer : OpenApiSecuritySchemes.Cookie;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(scheme, context.Document)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd(
            StatusCodes.Status401Unauthorized.ToString(),
            new OpenApiResponse { Description = usesBearer ? "Falta el token Bearer o no es válido." : "No hay una sesión iniciada en el sitio." });

        if (authorization.Any(data => !string.IsNullOrWhiteSpace(data.Roles) || !string.IsNullOrWhiteSpace(data.Policy)))
        {
            operation.Responses.TryAdd(
                StatusCodes.Status403Forbidden.ToString(),
                new OpenApiResponse { Description = "El rol de la cuenta no permite esta operación." });
        }
    }
}
