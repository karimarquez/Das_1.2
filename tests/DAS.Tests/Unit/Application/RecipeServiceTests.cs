using DAS.Application.DTOs;
using DAS.Domain.Enums;
using DAS.Tests.Fakes;

namespace DAS.Tests.Unit.Application;

public class RecipeServiceTests
{
    private readonly TestInventory _inventory = new();

    [Fact]
    public async Task Preparar_ConvierteLasUnidadesBaseADescuentosDeInventario()
    {
        var tequila = _inventory.AddProduct("Tequila blanco", stock: 2, baseUnit: "oz", equivalence: 25.36m);
        var juice = _inventory.AddProduct("Jugo de naranja", stock: 3, unit: "galones", baseUnit: "oz", equivalence: 128);
        var lime = _inventory.AddProduct("Limón", stock: 10, unit: "unidades", baseUnit: "unidades");
        var margarita = _inventory.AddRecipe("Margarita clásica", (tequila, 2), (juice, 1), (lime, 1));

        var result = await _inventory.RecipeService().PrepareAsync(new PrepareRecipeDto(margarita.Id, 3));

        Assert.True(result.Succeeded);
        Assert.Equal(1.7634m, tequila.Stock);
        Assert.Equal(2.9766m, juice.Stock);
        Assert.Equal(7, lime.Stock);
        Assert.Equal(3, _inventory.Movements.Items.Count);
        Assert.All(_inventory.Movements.Items, movement =>
        {
            Assert.Equal(MovementType.Consumo, movement.Type);
            Assert.Equal("Receta: Margarita clásica · 3 porción(es)", movement.Reason);
        });
    }

    [Fact]
    public async Task Preparar_ConUnIngredienteInsuficiente_NoDescuentaNinguno()
    {
        var beer = _inventory.AddProduct("Cerveza Pilsener", stock: 10);
        var lime = _inventory.AddProduct("Limón", stock: 1, unit: "unidades", baseUnit: "unidades");
        var michelada = _inventory.AddRecipe("Michelada", (beer, 1), (lime, 1));

        var result = await _inventory.RecipeService().PrepareAsync(new PrepareRecipeDto(michelada.Id, 3));

        Assert.False(result.Succeeded);
        Assert.Contains("Limón", result.Error);
        Assert.Equal(10, beer.Stock);
        Assert.Equal(1, lime.Stock);
        Assert.Empty(_inventory.Movements.Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task Preparar_ConPorcionesFueraDeRango_Falla(int servings)
    {
        var beer = _inventory.AddProduct("Cerveza Pilsener", stock: 10);
        var michelada = _inventory.AddRecipe("Michelada", (beer, 1));

        var result = await _inventory.RecipeService().PrepareAsync(new PrepareRecipeDto(michelada.Id, servings));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(PrepareRecipeDto.Servings), result.Field);
    }

    [Fact]
    public async Task Preparar_RecetaInexistente_Falla()
    {
        var result = await _inventory.RecipeService().PrepareAsync(new PrepareRecipeDto(42, 1));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Preparar_ConIngredienteInactivo_Falla()
    {
        var beer = _inventory.AddProduct("Cerveza Pilsener", stock: 10, active: false);
        var michelada = _inventory.AddRecipe("Michelada", (beer, 1));

        var result = await _inventory.RecipeService().PrepareAsync(new PrepareRecipeDto(michelada.Id, 1));

        Assert.False(result.Succeeded);
        Assert.Equal(10, beer.Stock);
    }

    [Fact]
    public async Task Preparar_ConUnidadDistintaALaDelProducto_Falla()
    {
        var tequila = _inventory.AddProduct("Tequila blanco", stock: 2, baseUnit: "oz", equivalence: 25.36m);
        var recipe = _inventory.AddRecipe("Tequila derecho", (tequila, 2));
        recipe.Ingredients[0].BaseUnit = "ml";

        var result = await _inventory.RecipeService().PrepareAsync(new PrepareRecipeDto(recipe.Id, 1));

        Assert.False(result.Succeeded);
        Assert.Contains("No se pudo convertir", result.Error);
        Assert.Equal(2, tequila.Stock);
    }

    [Fact]
    public async Task Crear_ConIngredienteRepetido_Falla()
    {
        var lime = _inventory.AddProduct("Limón", stock: 10);

        var result = await _inventory.RecipeService().CreateAsync(new SaveRecipeDto("Limonada", "", [new(lime.Id, 2), new(lime.Id, 1)]));

        Assert.False(result.Succeeded);
        Assert.Empty(_inventory.Recipes.Items);
    }

    [Fact]
    public async Task Crear_ConNombreExistente_Falla()
    {
        var lime = _inventory.AddProduct("Limón", stock: 10);
        _inventory.AddRecipe("Limonada", (lime, 2));

        var result = await _inventory.RecipeService().CreateAsync(new SaveRecipeDto("limonada", "", [new(lime.Id, 2)]));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(SaveRecipeDto.Name), result.Field);
    }

    [Fact]
    public async Task Crear_GuardaLaUnidadBaseVigenteDelProducto()
    {
        var sugar = _inventory.AddProduct("Azúcar", stock: 3, unit: "bolsas", baseUnit: "lb", equivalence: 5);

        var result = await _inventory.RecipeService().CreateAsync(new SaveRecipeDto("Agua dulce", "Prueba", [new(sugar.Id, 0.05m)]));

        Assert.True(result.Succeeded);
        var recipe = Assert.Single(_inventory.Recipes.Items);
        Assert.Equal("lb", Assert.Single(recipe.Ingredients).BaseUnit);
        Assert.Equal("Azúcar", Assert.Single(result.Value!.Ingredients).ProductName);
    }
}
