using DAS.Application.Services;
using DAS.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DAS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        //Una sola instancia por petición atiende tanto las consultas como los comandos de productos
        services.AddScoped<ProductService>();
        services.AddScoped<IProductService>(provider => provider.GetRequiredService<ProductService>());
        services.AddScoped<IProductQueryService>(provider => provider.GetRequiredService<ProductService>());

        services.AddScoped<IMovementService, MovementService>();
        services.AddScoped<IPhysicalCountService, PhysicalCountService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
