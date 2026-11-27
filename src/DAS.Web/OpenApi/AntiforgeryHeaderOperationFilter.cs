using DAS.Web.Security;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DAS.Web.OpenApi;

public sealed class AntiforgeryHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        if (method is null || HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method)
            || !AntiforgeryMetadata.RequiresToken(context.ApiDescription.ActionDescriptor.EndpointMetadata))
        {
            return;
        }

        operation.Parameters ??= new List<IOpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = SecurityPolicies.AntiforgeryHeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Token antifalsificación (CSRF) de la sesión. Cada página del sitio lo publica en <meta name=\"csrf-token\">.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }
}
