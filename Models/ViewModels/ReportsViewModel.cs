using DAS.Models.DTOs;
using DAS.Models.Enums;

namespace DAS.Models.ViewModels;

public class ReportsViewModel
{
    public ReportPeriod Period { get; init; }
    public DateTime ReferenceDate { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public string PeriodLabel { get; init; } = string.Empty;
    public IReadOnlyList<MovementDto> Movements { get; init; } = [];
    public IReadOnlyList<ProductReportLineDto> Lines { get; init; } = [];
    public int EntryCount { get; init; }
    public int ConsumptionCount { get; init; }
    public int WasteCount { get; init; }
    public int AdjustmentCount { get; init; }
    public int ProductsConsumedCount { get; init; }
    public decimal ConsumptionCost { get; init; }
    public decimal WasteCost { get; init; }

    public bool IsDaily => Period == ReportPeriod.Diario;
    public string PeriodRouteValue => IsDaily ? "diario" : "mensual";
}
