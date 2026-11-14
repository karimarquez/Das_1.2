using DAS.Domain.Entities;
using DAS.Domain.Enums;
using DAS.Tests.Fakes;

namespace DAS.Tests.Unit.Application;

public class ReportServiceTests
{
    private readonly TestInventory _inventory = new();
    private readonly InventoryProduct _beer;
    private readonly InventoryProduct _lime;

    public ReportServiceTests()
    {
        _beer = _inventory.AddProduct("Cerveza Pilsener", cost: 0.85m);
        _lime = _inventory.AddProduct("Limón", unit: "unidades", baseUnit: "unidades", cost: 0.08m);

        Record(_beer, MovementType.Entrada, 6, new DateTime(2026, 10, 31, 20, 0, 0));
        Record(_beer, MovementType.Entrada, 24, new DateTime(2026, 11, 3, 10, 0, 0));
        Record(_beer, MovementType.Consumo, 12, new DateTime(2026, 11, 10, 21, 30, 0));
        Record(_beer, MovementType.Merma, 2, new DateTime(2026, 11, 14, 9, 15, 0));
        Record(_lime, MovementType.Entrada, 50, new DateTime(2026, 11, 14, 8, 40, 0));
        Record(_lime, MovementType.Consumo, 10, new DateTime(2026, 11, 14, 12, 0, 0));
    }

    [Fact]
    public async Task Mensual_ResumeEntradasConsumosYMermasConSusCostos()
    {
        var report = await _inventory.ReportService().BuildAsync(ReportPeriod.Mensual, new DateTime(2026, 11, 20));

        Assert.Equal(new DateTime(2026, 11, 1), report.From);
        Assert.Equal(new DateTime(2026, 11, 30), report.To);
        Assert.Equal(2, report.EntryCount);
        Assert.Equal(2, report.ConsumptionCount);
        Assert.Equal(1, report.WasteCount);
        Assert.Equal(2, report.ProductsConsumedCount);

        var beer = Assert.Single(report.Lines, line => line.ProductId == _beer.Id);
        Assert.Equal(24, beer.Entries);
        Assert.Equal(12, beer.Consumption);
        Assert.Equal(2, beer.Waste);
        Assert.Equal(10.20m, beer.ConsumptionCost);
        Assert.Equal(1.70m, beer.WasteCost);
        Assert.Equal(10.20m + 0.80m, report.ConsumptionCost);
    }

    [Fact]
    public async Task Diario_SoloIncluyeLosMovimientosDeEseDia()
    {
        var report = await _inventory.ReportService().BuildAsync(ReportPeriod.Diario, new DateTime(2026, 11, 14));

        Assert.Equal(3, report.Movements.Count);
        Assert.All(report.Movements, movement => Assert.Equal(new DateTime(2026, 11, 14), movement.RecordedAt.Date));
        var beer = Assert.Single(report.Lines, line => line.ProductId == _beer.Id);
        Assert.Equal(0, beer.Entries);
        Assert.Equal(2, beer.Waste);
    }

    [Fact]
    public async Task ProductoInactivoConMovimientos_SigueApareciendoEnElReporte()
    {
        _lime.IsActive = false;

        var report = await _inventory.ReportService().BuildAsync(ReportPeriod.Mensual, new DateTime(2026, 11, 14));

        Assert.Contains(report.Lines, line => line.ProductId == _lime.Id);
    }

    private void Record(InventoryProduct product, MovementType type, decimal quantity, DateTime recordedAt) =>
        _inventory.Movements.Seed(product.RegisterMovement(type, quantity, "Prueba de reporte", TestData.UserName, recordedAt));
}
