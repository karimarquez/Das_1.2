using DAS.Infrastructure.Identity;
using DAS.Infrastructure.Persistence;
using DAS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAS.Infrastructure;

public static class DatabaseInitializer
{
    //Aplica las migraciones pendientes y carga los datos iniciales al arrancar la aplicación
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
        await InventorySeeder.SeedAsync(context, provider.GetRequiredService<TimeProvider>(), cancellationToken);
        await IdentityDataSeeder.SeedAsync(provider, configuration);
    }
}
