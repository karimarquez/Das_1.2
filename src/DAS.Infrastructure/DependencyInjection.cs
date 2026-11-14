using DAS.Application.Abstractions;
using DAS.Application.Abstractions.Persistence;
using DAS.Infrastructure.Persistence;
using DAS.Infrastructure.Persistence.Interceptors;
using DAS.Infrastructure.Persistence.Repositories;
using DAS.Infrastructure.Reports;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DAS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();

        //La cadena de conexión se lee al crear el contexto, así las pruebas pueden reemplazarla
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseSqlite(configuration.GetConnectionString("DefaultConnection"))
            .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IPhysicalCountRepository, PhysicalCountRepository>();

        services.AddSingleton<IReportExporter, CsvReportExporter>();
        return services;
    }

    //Identity guarda usuarios y roles en el mismo AppDbContext; la capa web no necesita conocerlo
    public static IdentityBuilder AddInfrastructureStores(this IdentityBuilder builder) =>
        builder.AddEntityFrameworkStores<AppDbContext>();
}
