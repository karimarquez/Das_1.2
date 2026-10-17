using DAS.Models.DTOs;
using DAS.Models.ViewModels;

namespace DAS.Mappings;

public static class ReportMappings
{
    public static ReportsViewModel ToViewModel(this InventoryReportDto report, DateTime referenceDate) => new()
    {
        Period = report.Period,
        ReferenceDate = referenceDate,
        From = report.From,
        To = report.To,
        PeriodLabel = report.Label,
        Movements = report.Movements,
        Lines = report.Lines,
        EntryCount = report.EntryCount,
        ConsumptionCount = report.ConsumptionCount,
        WasteCount = report.WasteCount,
        AdjustmentCount = report.AdjustmentCount,
        ProductsConsumedCount = report.ProductsConsumedCount,
        ConsumptionCost = report.ConsumptionCost,
        WasteCost = report.WasteCost
    };
}
