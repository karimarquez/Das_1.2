using DAS.Configuration;
using DAS.Data;
using DAS.Services;
using DAS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAS.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InventoryOptions>()
            .Bind(configuration.GetSection(InventoryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        //Scoped: una instancia de los servicios y del contexto por petición HTTP.
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IMovementService, MovementService>();
        services.AddScoped<IPhysicalCountService, PhysicalCountService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }
}
