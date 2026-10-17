using DAS.Models.DTOs;

namespace DAS.Models.ViewModels;

public class InventoryDashboardViewModel
{
    public IReadOnlyList<ProductDto> Products { get; init; } = [];
    public IReadOnlyList<MovementDto> RecentMovements { get; init; } = [];
    public int ProductCount { get; init; }
    public int LowStockCount { get; init; }
    public int TodayMovementCount { get; init; }
    public int TodayConsumptionCount { get; init; }
}
