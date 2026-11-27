using DAS.Web.OpenApi;
using Microsoft.OpenApi;

namespace DAS.Web.Extensions;

public static class ApiDocumentationExtensions
{
    public const string DocumentName = "v1";
    public const string EnabledSetting = "Swagger:Habilitado";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = "Pipiris Nais · API de inventario",
                Version = DocumentName,
                Description = "Servicios REST del inventario de bar. Los endpoints marcados como JWT se usan desde clientes externos " +
                              "con un token Bearer obtenido en POST /api/auth/token. Los endpoints de sesión web son los que consume " +
                              "el propio sitio con Fetch y jQuery AJAX: usan la cookie de la sesión iniciada y, si modifican datos, " +
                              "el encabezado X-CSRF-TOKEN. Los errores se devuelven como ProblemDetails (RFC 9457)."
            });

            options.DocInclusionPredicate((_, description) =>
                description.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true);
            options.TagActionsBy(ApiTags.For);

            options.AddSecurityDefinition(OpenApiSecuritySchemes.Bearer, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Pega solo el access_token que devuelve POST /api/auth/token."
            });
            options.AddSecurityDefinition(OpenApiSecuritySchemes.Cookie, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Name = OpenApiSecuritySchemes.CookieName,
                Description = "Cookie de la sesión iniciada en /cuenta/ingresar. El navegador la envía sola desde el mismo sitio."
            });

            options.OperationFilter<SecurityRequirementsOperationFilter>();
            options.OperationFilter<AntiforgeryHeaderOperationFilter>();
            options.DocumentFilter<TagDescriptionsDocumentFilter>();
            options.IncludeXmlComments(typeof(Program).Assembly);
            options.SupportNonNullableReferenceTypes();
        });

        return services;
    }

    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        var enabled = app.Configuration.GetValue(
            EnabledSetting,
            app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"));
        if (!enabled)
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"{DocumentName}/swagger.json", "API de inventario v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Pipiris Nais · Documentación de la API";
            options.DisplayRequestDuration();
        });

        return app;
    }
}
