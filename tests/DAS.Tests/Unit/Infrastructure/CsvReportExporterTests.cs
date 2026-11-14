using System.Globalization;
using System.Text;
using DAS.Application.DTOs;
using DAS.Infrastructure.Reports;

namespace DAS.Tests.Unit.Infrastructure;

public class CsvReportExporterTests
{
    private readonly CsvReportExporter _exporter = new();

    [Fact]
    public void Exportar_IniciaConBomYEncabezado()
    {
        var bytes = _exporter.Export(Report());

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes.Take(3).ToArray());
        Assert.StartsWith("Producto,Categoría,Unidad,Entradas", Encoding.UTF8.GetString(bytes[3..]));
    }

    [Fact]
    public void Exportar_EscapaComasYComillas()
    {
        var csv = Encoding.UTF8.GetString(_exporter.Export(Report("Vino \"Tinto\", reserva")));

        Assert.Contains("\"Vino \"\"Tinto\"\", reserva\"", csv);
    }

    [Fact]
    public void Exportar_UsaPuntoDecimalSinImportarLaCulturaDelEquipo()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-ES");
            var csv = Encoding.UTF8.GetString(_exporter.Export(Report()));

            Assert.Contains(",1.5,", csv);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static InventoryReportDto Report(string productName = "Tequila blanco") => new()
    {
        Lines =
        [
            new ProductReportLineDto
            {
                ProductId = 1,
                ProductName = productName,
                Category = "Licores",
                Unit = "botellas",
                Entries = 6,
                Consumption = 1.5m,
                Waste = 0,
                CurrentStock = 4.5m,
                ConsumptionCost = 27.75m
            }
        ]
    };
}
