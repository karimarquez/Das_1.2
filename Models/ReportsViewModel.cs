namespace DAS.Models;

public class ReportsViewModel
{
    public IReadOnlyList<InventoryMovement> Movements { get; init; } = [];
    public int EntryCount { get; init; }
    public int ConsumptionCount { get; init; }
    public int WasteCount { get; init; }
    public int ProductsConsumedCount { get; init; }
    public string PeriodLabel { get; init; } = string.Empty;
    public bool IsDaily { get; init; }
}