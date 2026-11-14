using DAS.Application.DTOs;
using DAS.Domain.Enums;
using DAS.Tests.Fakes;

namespace DAS.Tests.Unit.Application;

public class ProductServiceTests
{
    private readonly TestInventory _inventory = new();

    [Fact]
    public async Task Crear_ConNombreDuplicado_FallaEnElCampoNombreSinGuardar()
    {
        _inventory.AddProduct("Tequila blanco");

        var result = await _inventory.ProductService().CreateAsync(TestData.NewProduct("tequila BLANCO"));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(SaveProductDto.Name), result.Field);
        Assert.Equal(0, _inventory.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Crear_ConExistenciaInicial_RegistraLaEntradaANombreDelUsuario()
    {
        var result = await _inventory.ProductService().CreateAsync(TestData.NewProduct("Mezcal", initialStock: 5));

        Assert.True(result.Succeeded);
        var product = Assert.Single(_inventory.Products.Items);
        Assert.Equal(5, product.Stock);
        var movement = Assert.Single(_inventory.Movements.Items);
        Assert.Equal(MovementType.Entrada, movement.Type);
        Assert.Equal(product.Id, movement.ProductId);
        Assert.Equal("Existencia inicial", movement.Reason);
        Assert.Equal(TestData.UserName, movement.UserName);
        Assert.Equal(TestData.Now, movement.RecordedAt);
        Assert.Equal(1, _inventory.UnitOfWork.TransactionCount);
    }

    [Fact]
    public async Task Crear_SinExistenciaInicial_NoRegistraMovimientos()
    {
        var result = await _inventory.ProductService().CreateAsync(TestData.NewProduct("Mezcal"));

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Value!.Stock);
        Assert.Empty(_inventory.Movements.Items);
    }

    [Fact]
    public async Task Actualizar_ProductoInexistente_Falla()
    {
        var result = await _inventory.ProductService().UpdateAsync(99, TestData.NewProduct("Mezcal"));

        Assert.False(result.Succeeded);
        Assert.Equal("El producto no existe.", result.Error);
    }

    [Fact]
    public async Task Actualizar_CambiaElCatalogoPeroNoLaExistencia()
    {
        var product = _inventory.AddProduct("Tequila blanco", stock: 4);

        var result = await _inventory.ProductService().UpdateAsync(product.Id, TestData.NewProduct("Tequila reposado", initialStock: 50));

        Assert.True(result.Succeeded);
        Assert.Equal("Tequila reposado", product.Name);
        Assert.Equal(4, product.Stock);
        Assert.Empty(_inventory.Movements.Items);
    }

    [Fact]
    public async Task Actualizar_ConElNombreDeOtroProducto_Falla()
    {
        _inventory.AddProduct("Tequila blanco");
        var rum = _inventory.AddProduct("Ron añejo");

        var result = await _inventory.ProductService().UpdateAsync(rum.Id, TestData.NewProduct("Tequila Blanco"));

        Assert.False(result.Succeeded);
        Assert.Equal("Ron añejo", rum.Name);
    }

    [Fact]
    public async Task Desactivar_ConservaElProductoYCambiaSuEstado()
    {
        var product = _inventory.AddProduct("Pajillas", stock: 3);

        var result = await _inventory.ProductService().SetActiveAsync(product.Id, false);

        Assert.True(result.Succeeded);
        Assert.False(product.IsActive);
        Assert.Single(_inventory.Products.Items);
    }

    [Fact]
    public async Task Activos_SoloIncluyeProductosActivosOrdenadosPorNombre()
    {
        _inventory.AddProduct("Vasos acrílicos");
        _inventory.AddProduct("Agua mineral");
        _inventory.AddProduct("Pajillas", active: false);

        var products = await _inventory.ProductService().GetActiveAsync();

        Assert.Equal(new[] { "Agua mineral", "Vasos acrílicos" }, products.Select(product => product.Name));
    }

    [Fact]
    public async Task BuscarExistencias_LimitaLaPaginaYElTamanio()
    {
        _inventory.AddProduct("Agua mineral");

        await _inventory.ProductService().SearchStockAsync(null, page: 0, pageSize: 500);

        Assert.Equal((1, 100), _inventory.Products.LastSearchPaging);
    }
}
