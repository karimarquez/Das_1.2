using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using DAS.Web.Models.Validation;
using DAS.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DAS.Web.Extensions;

public static class ProtectionExtensions
{
    private static readonly IReadOnlyDictionary<int, string> ProblemTitles = new Dictionary<int, string>
    {
        [StatusCodes.Status400BadRequest] = "La solicitud no es válida.",
        [StatusCodes.Status401Unauthorized] = "Inicia sesión para continuar.",
        [StatusCodes.Status403Forbidden] = "No tienes permiso para realizar esta acción.",
        [StatusCodes.Status404NotFound] = "El recurso solicitado no existe.",
        [StatusCodes.Status405MethodNotAllowed] = "El método HTTP no está permitido para este recurso.",
        [StatusCodes.Status409Conflict] = "La operación entra en conflicto con el estado actual.",
        [StatusCodes.Status415UnsupportedMediaType] = "El tipo de contenido no es compatible; envía JSON.",
        [StatusCodes.Status429TooManyRequests] = "Demasiadas solicitudes. Espera un momento e inténtalo de nuevo.",
        [StatusCodes.Status500InternalServerError] = "Ocurrió un error inesperado."
    };

    private static readonly HashSet<string> DefaultProblemTitles = new(StringComparer.Ordinal)
    {
        "One or more validation errors occurred.",
        "An error occurred while processing your request."
    };

    public static IServiceCollection AddApplicationProtection(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KestrelServerOptions>(options => options.AddServerHeader = false);

        services.AddAntiforgery(options =>
        {
            options.HeaderName = SecurityPolicies.AntiforgeryHeaderName;
            options.SuppressXFrameOptionsHeader = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        services.Configure<MvcOptions>(options => options.Conventions.Add(new ViewControllersAntiforgeryConvention()));
        services.Replace(ServiceDescriptor.Singleton<IValidationAttributeAdapterProvider, LocalValidationAttributeAdapterProvider>());
        var jsonEncoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Encoder = jsonEncoder;
        });
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.Encoder = jsonEncoder);

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            if (problem.Status is not int status || !IsDefaultTitle(problem.Title, status))
            {
                return;
            }

            if (problem is HttpValidationProblemDetails)
            {
                problem.Title = "Los datos enviados no son válidos.";
            }
            else if (ProblemTitles.TryGetValue(status, out var title))
            {
                problem.Title = title;
            }
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(SecurityPolicies.CredentialsRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, configuration.GetValue(
                        SecurityPolicies.CredentialAttemptsSetting,
                        SecurityPolicies.DefaultCredentialAttemptsPerMinute)),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
            options.OnRejected = WriteRateLimitResponseAsync;
        });

        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();

    public static IApplicationBuilder UseStatusCodeResponses(this IApplicationBuilder app)
    {
        app.UseWhen(context => !SecurityPolicies.IsApiRequest(context.Request), branch => branch.UseStatusCodePagesWithReExecute("/error/{0}"));
        app.UseWhen(context => SecurityPolicies.IsApiRequest(context.Request), branch => branch.UseStatusCodePages());
        return app;
    }

    private static bool IsDefaultTitle(string? title, int status) =>
        string.IsNullOrWhiteSpace(title)
        || DefaultProblemTitles.Contains(title)
        || string.Equals(title, ReasonPhrases.GetReasonPhrase(status), StringComparison.Ordinal);

    private static async ValueTask WriteRateLimitResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        if (!SecurityPolicies.IsApiRequest(httpContext.Request))
        {
            return;
        }

        var problems = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Detail = "Se superó el número de intentos permitidos por minuto. Espera antes de volver a intentarlo."
            }
        });
    }
}
