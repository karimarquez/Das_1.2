using DAS.Application.DTOs;
using DAS.Application.Validation;
using DAS.Tests.Fakes;

namespace DAS.Tests.Unit.Application;

public class InputValidationTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Ron añejo 7 años · Botella 750 ml", true)]
    [InlineData("Precio > costo", false)]
    [InlineData("<script>alert(1)</script>", false)]
    [InlineData("<img src=x onerror=alert(1)>", false)]
    public void TextoPlano_RechazaLosSignosDeEtiquetasHtml(string? value, bool expected)
    {
        Assert.Equal(expected, new PlainTextAttribute().IsValid(value));
    }

    [Fact]
    public void TextoPlano_UsaElNombreVisibleEnElMensaje()
    {
        Assert.Equal(
            "Motivo no puede contener etiquetas HTML ni los caracteres < o >.",
            new PlainTextAttribute().FormatErrorMessage("Motivo"));
    }

    [Fact]
    public void ProductoValido_PasaLaValidacion()
    {
        Assert.True(InputValidator.Validate(TestData.NewProduct("Mezcal")).Succeeded);
    }

    [Theory]
    [InlineData("", nameof(SaveProductDto.Name))]
    [InlineData("M", nameof(SaveProductDto.Name))]
    [InlineData("<b>Mezcal</b>", nameof(SaveProductDto.Name))]
    public void NombreInvalido_FallaEnElCampoNombre(string name, string field)
    {
        var result = InputValidator.Validate(TestData.NewProduct(name));

        Assert.False(result.Succeeded);
        Assert.Equal(field, result.Field);
    }

    [Fact]
    public void ValoresNumericosFueraDeRango_FallanEnSuCampo()
    {
        var result = InputValidator.Validate(TestData.NewProduct("Mezcal") with { UnitCost = -5 });

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(SaveProductDto.UnitCost), result.Field);
        Assert.Equal("El costo debe estar entre 0 y 100000.", result.Error);
    }

    [Fact]
    public void CategoriaFueraDelCatalogo_Falla()
    {
        var result = InputValidator.Validate(TestData.NewProduct("Mezcal") with { Category = "Electrónica" });

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(SaveProductDto.Category), result.Field);
    }

    [Fact]
    public void UnidadesIgualesConEquivalenciaDistintaDeUno_Falla()
    {
        var result = InputValidator.Validate(TestData.NewProduct("Mezcal") with { Unit = "botellas", BaseUnit = "botellas", BaseUnitsPerStockUnit = 2 });

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(SaveProductDto.BaseUnitsPerStockUnit), result.Field);
    }

    [Fact]
    public async Task Servicio_RechazaDatosInvalidosSinGuardar()
    {
        var inventory = new TestInventory();

        var result = await inventory.ProductService().CreateAsync(TestData.NewProduct("<script>Mezcal</script>"));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(SaveProductDto.Name), result.Field);
        Assert.Empty(inventory.Products.Items);
        Assert.Equal(0, inventory.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Servicio_ValidaTambienAlActualizar()
    {
        var inventory = new TestInventory();
        var product = inventory.AddProduct("Mezcal");

        var result = await inventory.ProductService().UpdateAsync(product.Id, TestData.NewProduct("Mezcal") with { Location = "<b>Barra</b>" });

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(SaveProductDto.Location), result.Field);
        Assert.Equal("Barra", product.Location);
    }
}
