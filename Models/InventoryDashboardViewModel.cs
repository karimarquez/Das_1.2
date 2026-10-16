namespace DAS.Models;

public class InventoryDashboardViewModel
{
    public IReadOnlyList<InventoryProduct> Products { get; init; } = [];
    public IReadOnlyList<InventoryMovement> RecentMovements { get; init; } = [];
    public int ProductCount { get; init; }
    public int LowStockCount { get; init; }
    public int TodayMovementCount { get; init; }
    public int TodayConsumptionCount { get; init; }
}