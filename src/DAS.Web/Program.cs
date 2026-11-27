using DAS.Application;
using DAS.Infrastructure;
using DAS.Web.Extensions;
using DAS.Web.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        options.UseUtcTimestamp = true;
    });
}

//Composición de capas: cada una registra sus propias dependencias
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddWebPresentation(builder.Configuration)
    .AddWebSecurity(builder.Configuration, builder.Environment)
    .AddApplicationProtection(builder.Configuration)
    .AddApiDocumentation();

builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(app.Configuration);

app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSpanishCulture();
app.UseStatusCodeResponses();
app.UseHttpsRedirection();
app.UseApiDocumentation();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = (context, report) =>
        context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() }, context.RequestAborted)
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = (context, report) =>
        context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() }, context.RequestAborted)
}).AllowAnonymous();

app.MapStaticAssets().AllowAnonymous();
app.MapControllers();

//Ruta convencional para HomeController; los módulos de inventario usan rutas por atributo
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inventory}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program { }
