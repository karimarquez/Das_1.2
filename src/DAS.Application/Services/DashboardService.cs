using DAS.Application.Abstractions.Persistence;
using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Application.Services.Interfaces;
using DAS.Domain.Enums;

namespace DAS.Application.Services;

public sealed class DashboardService(
    IProductRepository products,
    IMovementRepository movements,
    TimeProvider clock) : IDashboardService
{
    public const int MaxRecentMovements = 50;

    public async Task<DashboardDto> GetAsync(int recentMovements, CancellationToken cancellationToken = default)
    {
        var active = (await products.ListOrderedAsync(activeOnly: true, cancellationToken))
            .Select(product => product.ToDto())
            .ToList();

        var today = clock.GetLocalNow().Date;
        var todayCounts = await movements.CountByTypeAsync(today, today, cancellationToken);
        var recent = await movements.SearchAsync(
            new MovementQueryDto(Take: Math.Clamp(recentMovements, 1, MaxRecentMovements)),
            cancellationToken);

        return new DashboardDto(
            active,
            recent.Select(movement => movement.ToDto()).ToList(),
            active.Count,
            active.Count(product => product.IsLowStock),
            todayCounts.Sum(count => count.Count),
            todayCounts.Where(count => count.Type == MovementType.Consumo).Sum(count => count.Count));
    }
}
