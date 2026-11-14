using DAS.Domain.Entities;
using DAS.Domain.Enums;
using DAS.Domain.Exceptions;
using DAS.Tests.Fakes;

namespace DAS.Tests.Unit.Domain;

public class InventoryProductTests
{
    private static readonly DateTime Now = TestData.Now;

    [Fact]
    public void Entrada_AumentaLaExistenciaYDevuelveUnMovimientoTrazable()
    {
        var product = TestData.Product(1, "Tequila blanco", stock: 2);

        var movement = product.RegisterMovement(MovementType.Entrada, 6, "  Compra a proveedor ", "ana@pipiris.test", Now);

        Assert.Equal(8, product.Stock);
        Assert.Equal(1, movement.ProductId);
        Assert.Equal(MovementType.Entrada, movement.Type);
        Assert.Equal(6, movement.Quantity);
        Assert.Equal(2, movement.StockBefore);
        Assert.Equal(8, movement.StockAfter);
        Assert.Equal("Compra a proveedor", movement.Reason);
        Assert.Equal("ana@pipiris.test", movement.UserName);
        Assert.Equal(Now, movement.RecordedAt);
    }

    [Theory]
    [InlineData(MovementType.Consumo)]
    [InlineData(MovementType.Merma)]
    public void SalidaMayorQueLaExistencia_SeRechazaSinCambiarElSaldo(MovementType type)
    {
        var product = TestData.Product(1, "Cerveza Pilsener", stock: 3);

        var error = Assert.Throws<InsufficientStockException>(() => product.RegisterMovement(type, 5, "Cierre de turno", "ana", Now));

        Assert.Equal(3, product.Stock);
        Assert.Equal(3, error.Available);
        Assert.Contains("Cerveza Pilsener", error.Message);
    }

    [Fact]
    public void Merma_DescuentaLaExistencia()
    {
        var product = TestData.Product(1, "Limón", stock: 10, unit: "unidades", baseUnit: "unidades");

        var movement = product.RegisterMovement(MovementType.Merma, 3, "Producto dañado", "ana", Now);

        Assert.Equal(7, product.Stock);
        Assert.Equal(3, movement.Quantity);
        Assert.Equal(7, movement.StockAfter);
    }

    [Fact]
    public void Ajuste_NoSePuedeRegistrarComoMovimientoManual()
    {
        var product = TestData.Product(1, "Agua mineral", stock: 5);

        Assert.Throws<DomainException>(() => product.RegisterMovement(MovementType.Ajuste, 1, "Corrección", "ana", Now));
        Assert.Equal(5, product.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void CantidadNoPositiva_SeRechaza(int quantity)
    {
        var product = TestData.Product(1, "Agua mineral", stock: 5);

        Assert.Throws<DomainException>(() => product.RegisterMovement(MovementType.Entrada, quantity, "Prueba", "ana", Now));
    }

    [Fact]
    public void ProductoSinGuardar_NoPuedeRegistrarMovimientos()
    {
        var product = new InventoryProduct { Name = "Ron añejo", Unit = "botellas" };

        Assert.Throws<InvalidOperationException>(() => product.RegisterMovement(MovementType.Entrada, 1, "Compra", "ana", Now));
    }

    [Fact]
    public void ConteoConFaltante_GeneraUnAjusteNegativo()
    {
        var product = TestData.Product(1, "Tequila blanco", stock: 10);

        var adjustment = product.AdjustToCount(7, "Conteo físico #1 · faltante", "ana", Now);

        Assert.NotNull(adjustment);
        Assert.Equal(MovementType.Ajuste, adjustment!.Type);
        Assert.Equal(3, adjustment.Quantity);
        Assert.Equal(10, adjustment.StockBefore);
        Assert.Equal(7, adjustment.StockAfter);
        Assert.Equal(7, product.Stock);
    }

    [Fact]
    public void ConteoConSobrante_GeneraUnAjustePositivo()
    {
        var product = TestData.Product(1, "Tequila blanco", stock: 4);

        var adjustment = product.AdjustToCount(5.5m, "Conteo físico #1 · sobrante", "ana", Now);

        Assert.NotNull(adjustment);
        Assert.Equal(1.5m, adjustment!.Quantity);
        Assert.Equal(5.5m, product.Stock);
    }

    [Fact]
    public void ConteoSinDiferencia_NoGeneraMovimiento()
    {
        var product = TestData.Product(1, "Tequila blanco", stock: 4);

        Assert.Null(product.AdjustToCount(4, "Conteo físico #1", "ana", Now));
        Assert.Equal(4, product.Stock);
    }

    [Fact]
    public void ConversionDeReceta_UsaLaEquivalenciaDeLaPresentacion()
    {
        var tequila = TestData.Product(1, "Tequila blanco", stock: 2, baseUnit: "oz", equivalence: 25.36m);

        //3 margaritas de 2 oz = 6 oz; una botella tiene 25.36 oz
        Assert.Equal(0.2366m, tequila.ToStockUnits(6));
    }

    [Theory]
    [InlineData(4, 4, true)]
    [InlineData(3, 4, true)]
    [InlineData(5, 4, false)]
    public void ExistenciaEnOMenorAlMinimo_RequiereReposicion(int stock, int minimum, bool expected)
    {
        var product = TestData.Product(1, "Agua mineral", stock: stock, minimum: minimum);

        Assert.Equal(expected, product.IsLowStock);
    }

    [Fact]
    public void LineaDeConteo_GuardaElSaldoTeoricoYCalculaLaDiferencia()
    {
        var product = TestData.Product(7, "Pajillas", stock: 4, cost: 1.80m);
        var count = new PhysicalCount();

        var line = count.AddLine(product, 3);

        Assert.Equal(7, line.ProductId);
        Assert.Equal(4, line.TheoreticalStock);
        Assert.Equal(-1, line.Difference);
        Assert.Equal(1.80m, line.UnitCost);
        Assert.Throws<DomainException>(() => count.AddLine(product, -1));
    }
}
