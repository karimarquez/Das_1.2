using System.Globalization;
using System.Text;
using DAS.Application.Abstractions;
using DAS.Application.DTOs;

namespace DAS.Infrastructure.Reports;

public sealed class CsvReportExporter : IReportExporter
{
    public string Format => "csv";
    public string ContentType => "text/csv";
    public string FileExtension => "csv";

    public byte[] Export(InventoryReportDto report)
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

    private static string Number(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
