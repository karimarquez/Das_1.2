using DAS.Application.Services;
using DAS.Domain.Entities;
using DAS.Domain.Enums;
using DAS.Tests.Fakes;

namespace DAS.Tests.Unit.Application;

public class DashboardServiceTests
{
    private readonly TestInventory _inventory = new();

    [Fact]
    public async Task Resumen_CuentaActivosBajoMinimoYMovimientosDelDia()
    {
        var beer = _inventory.AddProduct("Cerveza Pilsener", stock: 2, minimum: 24);
        var water = _inventory.AddProduct("Agua mineral", stock: 30, minimum: 20);
        _inventory.AddProduct("Pajillas", stock: 1, minimum: 5, active: false);
        Record(water, MovementType.Consumo, 2, TestData.Now.AddDays(-1));
        Record(water, MovementType.Entrada, 5, TestData.Now.AddHours(-2));
        Record(beer, MovementType.Consumo, 1, TestData.Now.AddHours(-1));

        var dashboard = await Service().GetAsync(recentMovements: 2);

        Assert.Equal(2, dashboard.ProductCount);
        Assert.Equal(1, dashboard.LowStockCount);
        Assert.Equal(2, dashboard.TodayMovementCount);
        Assert.Equal(1, dashboard.TodayConsumptionCount);
        Assert.Equal(new[] { "Agua mineral", "Cerveza Pilsener" }, dashboard.Products.Select(product => product.Name));
        Assert.Equal(new[] { MovementType.Consumo, MovementType.Entrada }, dashboard.RecentMovements.Select(movement => movement.Type));
        Assert.Equal(33, dashboard.Products.Single(product => product.Name == "Agua mineral").Stock);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 3)]
    [InlineData(500, 4)]
    public async Task MovimientosRecientes_SeLimitanAlRangoPermitido(int requested, int expected)
    {
        var water = _inventory.AddProduct("Agua mineral", stock: 10);
        for (var index = 1; index <= 4; index++)
        {
            Record(water, MovementType.Entrada, 1, TestData.Now.AddMinutes(-index));
        }

        var dashboard = await Service().GetAsync(requested);

        Assert.Equal(expected, dashboard.RecentMovements.Count);
    }

    private DashboardService Service() => new(_inventory.Products, _inventory.Movements, _inventory.Clock);

    private void Record(InventoryProduct product, MovementType type, decimal quantity, DateTime recordedAt) =>
        _inventory.Movements.Seed(product.RegisterMovement(type, quantity, "Prueba de tablero", TestData.UserName, recordedAt));
}
