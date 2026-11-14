using DAS.Application.DTOs;
using DAS.Domain.Enums;
using DAS.Tests.Fakes;

namespace DAS.Tests.Unit.Application;

public class PhysicalCountServiceTests
{
    private readonly TestInventory _inventory = new();

    [Fact]
    public async Task Conteo_RegistraAjustesSoloParaLosProductosConDiferencia()
    {
        var tequila = _inventory.AddProduct("Tequila blanco", stock: 10, cost: 18.50m);
        var water = _inventory.AddProduct("Agua mineral", stock: 5);
        var straws = _inventory.AddProduct("Pajillas", stock: 3);

        var result = await _inventory.PhysicalCountService().RegisterAsync(
            [new CountLineDto(tequila.Id, 8), new CountLineDto(water.Id, 5), new CountLineDto(straws.Id, 4)]);

        Assert.True(result.Succeeded);
        var count = result.Value!;
        Assert.Equal(3, count.Lines.Count);
        Assert.Equal(8, tequila.Stock);
        Assert.Equal(5, water.Stock);
        Assert.Equal(4, straws.Stock);

        Assert.Equal(2, _inventory.Movements.Items.Count);
        Assert.All(_inventory.Movements.Items, movement => Assert.Equal(MovementType.Ajuste, movement.Type));
        Assert.Contains(_inventory.Movements.Items, movement => movement.Reason == $"Conteo físico #{count.Id} · faltante");
        Assert.Contains(_inventory.Movements.Items, movement => movement.Reason == $"Conteo físico #{count.Id} · sobrante");

        var shortage = Assert.Single(count.Lines, line => line.ProductId == tequila.Id);
        Assert.Equal(-2, shortage.Difference);
        Assert.Equal(-37m, shortage.DifferenceCost);
        Assert.Equal(TestData.UserName, count.UserName);
    }

    [Fact]
    public async Task Conteo_ConProductoRepetido_Falla()
    {
        var water = _inventory.AddProduct("Agua mineral", stock: 5);

        var result = await _inventory.PhysicalCountService().RegisterAsync([new CountLineDto(water.Id, 4), new CountLineDto(water.Id, 3)]);

        Assert.False(result.Succeeded);
        Assert.Equal(5, water.Stock);
    }

    [Fact]
    public async Task Conteo_ConCantidadNegativa_Falla()
    {
        var water = _inventory.AddProduct("Agua mineral", stock: 5);

        var result = await _inventory.PhysicalCountService().RegisterAsync([new CountLineDto(water.Id, -1)]);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Conteo_ConProductoInactivo_NoGuardaNada()
    {
        var water = _inventory.AddProduct("Agua mineral", stock: 5);
        var straws = _inventory.AddProduct("Pajillas", stock: 3, active: false);

        var result = await _inventory.PhysicalCountService().RegisterAsync([new CountLineDto(water.Id, 4), new CountLineDto(straws.Id, 2)]);

        Assert.False(result.Succeeded);
        Assert.Empty(_inventory.Counts.Items);
        Assert.Equal(5, water.Stock);
    }

    [Fact]
    public async Task Conteo_SinLineas_Falla()
    {
        var result = await _inventory.PhysicalCountService().RegisterAsync([]);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ConteosRecientes_SeDevuelvenDelMasNuevoAlMasAntiguo()
    {
        var water = _inventory.AddProduct("Agua mineral", stock: 5);
        var service = _inventory.PhysicalCountService();
        var first = await service.RegisterAsync([new CountLineDto(water.Id, 5)]);
        var second = await service.RegisterAsync([new CountLineDto(water.Id, 4)]);

        var recent = await service.GetRecentAsync(5);

        Assert.Equal(new[] { second.Value!.Id, first.Value!.Id }, recent.Select(count => count.Id));
    }
}
