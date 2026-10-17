using DAS.Configuration;
using DAS.Data;
using DAS.Services;
using DAS.Services.Interfaces;

namespace DAS.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InventoryOptions>()
            .Bind(configuration.GetSection(InventoryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        //Singleton: los datos en memoria deben conservarse entre peticiones
        services.AddSingleton<InMemoryInventoryStore>();

        //Scoped: una instancia por petición HTTP; los controladores dependen solo de las interfaces
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IMovementService, MovementService>();
        services.AddScoped<IPhysicalCountService, PhysicalCountService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
