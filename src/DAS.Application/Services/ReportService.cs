using System.Globalization;
using DAS.Application.Abstractions.Persistence;
using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Application.Services.Interfaces;
using DAS.Domain.Entities;
using DAS.Domain.Enums;

namespace DAS.Application.Services;

public sealed class ReportService(IProductRepository products, IMovementRepository movements) : IReportService
{
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-SV");

    public async Task<InventoryReportDto> BuildAsync(ReportPeriod period, DateTime referenceDate, CancellationToken cancellationToken = default)
    {
        var isDaily = period == ReportPeriod.Diario;
        var from = isDaily ? referenceDate.Date : new DateTime(referenceDate.Year, referenceDate.Month, 1);
        var to = isDaily ? from : from.AddMonths(1).AddDays(-1);
        var label = isDaily
            ? from.ToString("dddd, dd 'de' MMMM 'de' yyyy", SpanishCulture)
            : from.ToString("MMMM 'de' yyyy", SpanishCulture);

        var periodMovements = await movements.SearchAsync(new MovementQueryDto(From: from, To: to), cancellationToken);
        var byProduct = periodMovements.ToLookup(movement => movement.ProductId);
        var reportProducts = await products.ListForReportAsync(byProduct.Select(group => group.Key).ToList(), cancellationToken);

        //Los totales se calculan en memoria porque SQLite guarda los decimales como texto
        var lines = reportProducts
            .Select(product => BuildLine(product, byProduct[product.Id].ToList()))
            .OrderBy(line => line.ProductName)
            .ToList();

        return new InventoryReportDto
        {
            Period = period,
            From = from,
            To = to,
            Label = label,
            Movements = periodMovements.Select(movement => movement.ToDto()).ToList(),
            Lines = lines,
            EntryCount = periodMovements.Count(movement => movement.Type == MovementType.Entrada),
            ConsumptionCount = periodMovements.Count(movement => movement.Type == MovementType.Consumo),
            WasteCount = periodMovements.Count(movement => movement.Type == MovementType.Merma),
            AdjustmentCount = periodMovements.Count(movement => movement.Type == MovementType.Ajuste),
            ProductsConsumedCount = periodMovements.Where(movement => movement.Type == MovementType.Consumo).Select(movement => movement.ProductId).Distinct().Count(),
            ConsumptionCost = lines.Sum(line => line.ConsumptionCost),
            WasteCost = lines.Sum(line => line.WasteCost)
        };
    }

    private static ProductReportLineDto BuildLine(InventoryProduct product, IReadOnlyList<InventoryMovement> productMovements)
    {
        decimal Total(MovementType type) => productMovements.Where(movement => movement.Type == type).Sum(movement => movement.Quantity);

        var consumption = Total(MovementType.Consumo);
        var waste = Total(MovementType.Merma);
        return new ProductReportLineDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Category = product.Category,
            Unit = product.Unit,
            Entries = Total(MovementType.Entrada),
            Consumption = consumption,
            Waste = waste,
            Adjustments = productMovements.Where(movement => movement.Type == MovementType.Ajuste).Sum(movement => movement.StockAfter - movement.StockBefore),
            CurrentStock = product.Stock,
            ConsumptionCost = consumption * product.UnitCost,
            WasteCost = waste * product.UnitCost,
            IsLowStock = product.IsLowStock
        };
    }
}
