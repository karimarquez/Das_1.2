using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using DAS.Application.Abstractions;
using DAS.Infrastructure;
using DAS.Infrastructure.Identity;
using DAS.Web.Configuration;
using DAS.Web.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Tokens;

namespace DAS.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews(options => options.UseSpanishValidation());
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        //Las vistas escriben tildes y eñes tal cual en lugar de entidades HTML (&#xE1;)
        services.Configure<WebEncoderOptions>(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

        services.AddOptions<InventoryOptions>()
            .Bind(configuration.GetSection(InventoryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        return services;
    }

    public static IServiceCollection AddWebSecurity(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = !environment.IsDevelopment();
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = !environment.IsDevelopment();
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddInfrastructureStores()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/cuenta/ingresar";
            options.AccessDeniedPath = "/cuenta/acceso-denegado";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.SlidingExpiration = true;
        });

        //Sin clave configurada solo se permite una temporal en Desarrollo y en las pruebas automatizadas
        var jwtKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            {
                throw new InvalidOperationException("Configura Jwt:SigningKey mediante variables de entorno o un almacén de secretos.");
            }

            jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        configuration["Jwt:SigningKey"] = jwtKey;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        if (signingKey.KeySize < 256)
        {
            throw new InvalidOperationException("Jwt:SigningKey debe tener al menos 32 caracteres.");
        }

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services.AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "DAS",
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"] ?? "DAS.Api",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }
}
