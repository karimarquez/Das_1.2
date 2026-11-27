namespace DAS.Application.DTOs;

public sealed record DashboardDto(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<MovementDto> RecentMovements,
    int ProductCount,
    int LowStockCount,
    int TodayMovementCount,
    int TodayConsumptionCount);
