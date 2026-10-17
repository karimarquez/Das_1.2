using DAS.Models.Enums;

namespace DAS.Models.DTOs;

public sealed record ProductReportLineDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public decimal Entries { get; init; }
    public decimal Consumption { get; init; }
    public decimal Waste { get; init; }
    public decimal Adjustments { get; init; }
    public decimal CurrentStock { get; init; }
    public decimal ConsumptionCost { get; init; }
    public decimal WasteCost { get; init; }
    public bool IsLowStock { get; init; }
}

public sealed record InventoryReportDto
{
    public ReportPeriod Period { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public string Label { get; init; } = string.Empty;
    public IReadOnlyList<MovementDto> Movements { get; init; } = [];
    public IReadOnlyList<ProductReportLineDto> Lines { get; init; } = [];
    public int EntryCount { get; init; }
    public int ConsumptionCount { get; init; }
    public int WasteCount { get; init; }
    public int AdjustmentCount { get; init; }
    public int ProductsConsumedCount { get; init; }
    public decimal ConsumptionCost { get; init; }
    public decimal WasteCost { get; init; }
}
