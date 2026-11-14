using DAS.Application.DTOs;
using DAS.Domain.Enums;
using DAS.Tests.Fakes;

namespace DAS.Tests.Unit.Application;

public class MovementServiceTests
{
    private readonly TestInventory _inventory = new();

    [Fact]
    public async Task Ajuste_SeRechazaPorqueSoloLoGeneraElConteoFisico()
    {
        var product = _inventory.AddProduct("Agua mineral", stock: 5);

        var result = await _inventory.MovementService().RegisterAsync(new RegisterMovementDto(product.Id, MovementType.Ajuste, 1, "Corrección", null));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(RegisterMovementDto.Type), result.Field);
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public async Task ProductoInactivo_SeRechaza()
    {
        var product = _inventory.AddProduct("Agua mineral", stock: 5, active: false);

        var result = await _inventory.MovementService().RegisterAsync(new RegisterMovementDto(product.Id, MovementType.Entrada, 1, "Compra", null));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(RegisterMovementDto.ProductId), result.Field);
    }

    [Fact]
    public async Task ConsumoMayorQueLaExistencia_SeRechazaSinRegistrarNada()
    {
        var product = _inventory.AddProduct("Tequila blanco", stock: 2);

        var result = await _inventory.MovementService().RegisterAsync(new RegisterMovementDto(product.Id, MovementType.Consumo, 5, "Cierre de turno", null));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(RegisterMovementDto.Quantity), result.Field);
        Assert.Contains("Tequila blanco", result.Error);
        Assert.Equal(2, product.Stock);
        Assert.Empty(_inventory.Movements.Items);
        Assert.Equal(0, _inventory.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Entrada_ActualizaLaExistenciaYGuardaQuienLaRegistro()
    {
        var product = _inventory.AddProduct("Cerveza Pilsener", stock: 2);

        var result = await _inventory.MovementService().RegisterAsync(new RegisterMovementDto(product.Id, MovementType.Entrada, 24, "Reposición de proveedor", null));

        Assert.True(result.Succeeded);
        Assert.Equal(26, product.Stock);
        var movement = Assert.Single(_inventory.Movements.Items);
        Assert.Equal(TestData.UserName, movement.UserName);
        Assert.Equal(TestData.Now, movement.RecordedAt);
        Assert.Equal(movement.Id, result.Value!.Id);
        Assert.Equal(1, _inventory.UnitOfWork.TransactionCount);
    }

    [Fact]
    public async Task Merma_DescuentaLaExistencia()
    {
        var product = _inventory.AddProduct("Cerveza Pilsener", stock: 10);

        var result = await _inventory.MovementService().RegisterAsync(new RegisterMovementDto(product.Id, MovementType.Merma, 2, "Botella quebrada", null));

        Assert.True(result.Succeeded);
        Assert.Equal(8, product.Stock);
        Assert.False(result.Value!.IsIncrease);
    }

    [Fact]
    public async Task FechaPasada_ConservaElDiaConLaHoraActual()
    {
        var product = _inventory.AddProduct("Agua mineral", stock: 5);

        var result = await _inventory.MovementService().RegisterAsync(new RegisterMovementDto(product.Id, MovementType.Entrada, 1, "Compra atrasada", new DateTime(2026, 11, 10)));

        Assert.Equal(new DateTime(2026, 11, 10, 15, 30, 0), result.Value!.RecordedAt);
    }

    [Fact]
    public async Task FechaFutura_SeRegistraConLaFechaActual()
    {
        var product = _inventory.AddProduct("Agua mineral", stock: 5);

        var result = await _inventory.MovementService().RegisterAsync(new RegisterMovementDto(product.Id, MovementType.Entrada, 1, "Compra", new DateTime(2026, 12, 1)));

        Assert.Equal(TestData.Now, result.Value!.RecordedAt);
    }

    [Fact]
    public async Task Busqueda_FiltraPorProductoYOrdenaDelMasReciente()
    {
        var beer = _inventory.AddProduct("Cerveza Pilsener", stock: 10);
        var water = _inventory.AddProduct("Agua mineral", stock: 10);
        var service = _inventory.MovementService();
        await service.RegisterAsync(new RegisterMovementDto(beer.Id, MovementType.Consumo, 1, "Turno 1", new DateTime(2026, 11, 12)));
        await service.RegisterAsync(new RegisterMovementDto(water.Id, MovementType.Consumo, 1, "Turno 1", new DateTime(2026, 11, 12)));
        await service.RegisterAsync(new RegisterMovementDto(beer.Id, MovementType.Consumo, 2, "Turno 2", null));

        var movements = await service.SearchAsync(new MovementQueryDto(ProductId: beer.Id));

        Assert.Equal(new[] { "Turno 2", "Turno 1" }, movements.Select(movement => movement.Reason));
    }
}
