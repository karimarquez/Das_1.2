using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using DAS.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => options.UseSpanishValidation());
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
//Las vistas escriben tildes y eñes tal cual en lugar de entidades HTML (&#xE1;)
builder.Services.Configure<WebEncoderOptions>(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddInventoryModule(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//Cultura fija es-SV: el model binding interpreta los decimales con punto, igual que los input number
var culture = new CultureInfo("es-SV");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = [culture],
    SupportedUICultures = [culture]
});

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

//Ruta convencional para HomeController; los módulos de inventario usan rutas por atributo
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inventory}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
