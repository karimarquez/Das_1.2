using System.Globalization;
using System.Text;
using DAS.Data;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.Enums;
using DAS.Services.Interfaces;

namespace DAS.Services;

public sealed class ReportService(InMemoryInventoryStore store) : IReportService
{
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-SV");

    public InventoryReportDto Build(ReportPeriod period, DateTime referenceDate)
    {
        var isDaily = period == ReportPeriod.Diario;
        var from = isDaily ? referenceDate.Date : new DateTime(referenceDate.Year, referenceDate.Month, 1);
        var to = isDaily ? from : from.AddMonths(1).AddDays(-1);
        var label = isDaily
            ? from.ToString("dddd, dd 'de' MMMM 'de' yyyy", SpanishCulture)
            : from.ToString("MMMM 'de' yyyy", SpanishCulture);

        lock (store.SyncRoot)
        {
            var movements = store.Movements
                .Where(movement => movement.RecordedAt.Date >= from && movement.RecordedAt.Date <= to)
                .OrderByDescending(movement => movement.RecordedAt)
                .ThenByDescending(movement => movement.Id)
                .ToList();

            var lines = store.Products
                .Where(product => product.IsActive || movements.Any(movement => movement.ProductId == product.Id))
                .Select(product => BuildLine(product, movements.Where(movement => movement.ProductId == product.Id).ToList()))
                .OrderBy(line => line.ProductName)
                .ToList();

            return new InventoryReportDto
            {
                Period = period,
                From = from,
                To = to,
                Label = label,
                Movements = movements.Select(movement => movement.ToDto()).ToList(),
                Lines = lines,
                EntryCount = movements.Count(movement => movement.Type == MovementType.Entrada),
                ConsumptionCount = movements.Count(movement => movement.Type == MovementType.Consumo),
                WasteCount = movements.Count(movement => movement.Type == MovementType.Merma),
                AdjustmentCount = movements.Count(movement => movement.Type == MovementType.Ajuste),
                ProductsConsumedCount = movements.Where(movement => movement.Type == MovementType.Consumo).Select(movement => movement.ProductId).Distinct().Count(),
                ConsumptionCost = lines.Sum(line => line.ConsumptionCost),
                WasteCost = lines.Sum(line => line.WasteCost)
            };
        }
    }

    public byte[] ExportCsv(InventoryReportDto report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Producto,Categoría,Unidad,Entradas,Consumo,Mermas,Ajustes,Existencia actual,Costo de consumo,Costo de mermas");
        foreach (var line in report.Lines)
        {
            csv.AppendLine(string.Join(",",
                Escape(line.ProductName),
                Escape(line.Category),
                Escape(line.Unit),
                Number(line.Entries),
                Number(line.Consumption),
                Number(line.Waste),
                Number(line.Adjustments),
                Number(line.CurrentStock),
                Number(line.ConsumptionCost),
                Number(line.WasteCost)));
        }

        //BOM UTF-8 para que Excel muestre bien los acentos
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())];
    }

    private static ProductReportLineDto BuildLine(InventoryProduct product, IReadOnlyList<InventoryMovement> movements)
    {
        decimal Total(MovementType type) => movements.Where(movement => movement.Type == type).Sum(movement => movement.Quantity);

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
            Adjustments = movements.Where(movement => movement.Type == MovementType.Ajuste).Sum(movement => movement.StockAfter - movement.StockBefore),
            CurrentStock = product.Stock,
            ConsumptionCost = consumption * product.UnitCost,
            WasteCost = waste * product.UnitCost,
            IsLowStock = product.IsLowStock
        };
    }

    private static string Number(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
