using DAS.Application;
using DAS.Infrastructure;
using DAS.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

//Composición de capas: cada una registra sus propias dependencias
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddWebPresentation(builder.Configuration)
    .AddWebSecurity(builder.Configuration, builder.Environment)
    .AddApplicationProtection(builder.Configuration)
    .AddApiDocumentation();

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

app.MapStaticAssets().AllowAnonymous();
app.MapControllers();

//Ruta convencional para HomeController; los módulos de inventario usan rutas por atributo
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inventory}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program { }
