using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using DAS.Data;
using DAS.Extensions;
using DAS.Models.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => options.UseSpanishValidation());
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
//Las vistas escriben tildes y eñes tal cual en lugar de entidades HTML (&#xE1;)
builder.Services.Configure<WebEncoderOptions>(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = !builder.Environment.IsDevelopment();
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = !builder.Environment.IsDevelopment();
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/cuenta/ingresar";
    options.AccessDeniedPath = "/cuenta/acceso-denegado";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
});

var jwtKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Configura Jwt:SigningKey mediante variables de entorno o un almacén de secretos.");
    jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
builder.Configuration["Jwt:SigningKey"] = jwtKey;

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
if (signingKey.KeySize < 256)
    throw new InvalidOperationException("Jwt:SigningKey debe tener al menos 32 caracteres.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DAS";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DAS.Api";
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Products.AnyAsync())
    {
        var seed = new InMemoryInventoryStore();
        db.Products.AddRange(seed.Products);
        db.Movements.AddRange(seed.Movements);
        db.Recipes.AddRange(seed.Recipes);
        await db.SaveChangesAsync();
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Encargado", "Consulta" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = app.Configuration["Auth:BootstrapAdminEmail"];
    var adminPassword = app.Configuration["Auth:BootstrapAdminPassword"];
    if (!string.IsNullOrWhiteSpace(adminEmail) || !string.IsNullOrWhiteSpace(adminPassword))
    {
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            throw new InvalidOperationException("Configura tanto Auth:BootstrapAdminEmail como Auth:BootstrapAdminPassword.");
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException($"No se pudo crear el administrador inicial: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}

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
