using DAS.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DAS.Web.Health;

public sealed class DatabaseHealthCheck(AppDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await database.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("La base de datos no está disponible.");
    }
}
