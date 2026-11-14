using DAS.Application.DTOs;
using DAS.Web.Models.ViewModels;

namespace DAS.Web.Mappings;

public static class ReportViewModelMappings
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
