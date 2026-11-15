using DAS.Application.DTOs;
using DAS.Domain.Enums;
using DAS.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace DAS.Tests.Integration;

//Servicios reales + repositorios de EF Core + SQLite: el flujo de datos completo sin la capa web
public class ServiceFlowTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task AltaMovimientoYConteo_QuedanPersistidosConSuKardex()
    {
        int productId;
        await using (var context = _database.CreateContext())
        {
            var services = new EfServiceSet(context, _database.Clock);

            var created = await services.Products.CreateAsync(TestData.NewProduct("Ron añejo", initialStock: 10));
            productId = created.Value!.Id;
            var consumption = await services.Movements.RegisterAsync(new RegisterMovementDto(productId, MovementType.Consumo, 3, "Cierre de turno", null));
            var count = await services.Counts.RegisterAsync([new CountLineDto(productId, 6)]);

            Assert.True(created.Succeeded);
            Assert.True(consumption.Succeeded);
            Assert.True(count.Succeeded);
        }

        await using var verify = _database.CreateContext();
        var product = await verify.Products.SingleAsync(item => item.Id == productId);
        var movements = await verify.Movements.Where(movement => movement.ProductId == productId).OrderBy(movement => movement.Id).ToListAsync();
        var counts = await verify.PhysicalCounts.Include(item => item.Lines).ToListAsync();

        Assert.Equal(6, product.Stock);
        Assert.Equal(new[] { MovementType.Entrada, MovementType.Consumo, MovementType.Ajuste }, movements.Select(movement => movement.Type));
        Assert.Equal(new[] { 10m, 7m, 6m }, movements.Select(movement => movement.StockAfter));
        Assert.All(movements, movement => Assert.Equal(TestData.UserName, movement.UserName));
        Assert.Equal(7, Assert.Single(Assert.Single(counts).Lines).TheoreticalStock);
    }

    [Fact]
    public async Task RecetaConIngredienteInsuficiente_NoDejaMovimientosParciales()
    {
        int recipeId;
        await using (var context = _database.CreateContext())
        {
            var services = new EfServiceSet(context, _database.Clock);
            var beer = (await services.Products.CreateAsync(TestData.NewProduct("Cerveza artesanal", initialStock: 10) with { BaseUnit = "botellas", BaseUnitsPerStockUnit = 1 })).Value!;
            var lime = (await services.Products.CreateAsync(TestData.NewProduct("Limón persa", initialStock: 1) with { Unit = "unidades", BaseUnit = "unidades", BaseUnitsPerStockUnit = 1 })).Value!;
            var recipe = await services.Recipes.CreateAsync(new SaveRecipeDto("Michelada artesanal", "Prueba", [new(beer.Id, 1), new(lime.Id, 1)]));
            recipeId = recipe.Value!.Id;
        }

        await using (var context = _database.CreateContext())
        {
            var result = await new EfServiceSet(context, _database.Clock).Recipes.PrepareAsync(new PrepareRecipeDto(recipeId, 3));
            Assert.False(result.Succeeded);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(0, await verify.Movements.CountAsync(movement => movement.Type == MovementType.Consumo));
        Assert.Equal(10, (await verify.Products.SingleAsync(product => product.Name == "Cerveza artesanal")).Stock);
    }

    [Fact]
    public async Task ReporteMensual_SeConstruyeDesdeLaBaseDeDatos()
    {
        await using var context = _database.CreateContext();
        var services = new EfServiceSet(context, _database.Clock);
        var product = (await services.Products.CreateAsync(TestData.NewProduct("Vodka", initialStock: 5))).Value!;
        await services.Movements.RegisterAsync(new RegisterMovementDto(product.Id, MovementType.Merma, 1, "Botella quebrada", null));

        var report = await services.Reports.BuildAsync(ReportPeriod.Mensual, TestData.Now);

        var line = Assert.Single(report.Lines, item => item.ProductId == product.Id);
        Assert.Equal(1, report.EntryCount);
        Assert.Equal(1, report.WasteCount);
        Assert.Equal(5, line.Entries);
        Assert.Equal(1, line.Waste);
        Assert.Equal(15m, line.WasteCost);
        Assert.Equal(4, line.CurrentStock);
    }

    [Fact]
    public async Task ActualizarYEliminarReceta_PersisteCambiosYCascadaIngredientes()
    {
        int recipeId;
        int productId;
        await using (var context = _database.CreateContext())
        {
            var services = new EfServiceSet(context, _database.Clock);
            var firstProduct = (await services.Products.CreateAsync(TestData.NewProduct("Limón"))).Value!;
            var secondProduct = (await services.Products.CreateAsync(TestData.NewProduct("Azúcar"))).Value!;
            productId = secondProduct.Id;

            var recipe = new DAS.Domain.Entities.Recipe
            {
                Name = "Limonada",
                Description = "Original",
                Ingredients = [new() { ProductId = firstProduct.Id, QuantityPerServing = 1, BaseUnit = firstProduct.BaseUnit }]
            };
            context.Recipes.Add(recipe);
            await context.SaveChangesAsync();
            recipeId = recipe.Id;
        }

        await using (var context = _database.CreateContext())
        {
            var result = await new EfServiceSet(context, _database.Clock).Recipes.UpdateAsync(
                recipeId,
                new SaveRecipeDto("Limonada actualizada", "Nueva descripción", [new(productId, 0.5m)]));

            Assert.True(result.Succeeded);
        }

        await using (var verify = _database.CreateContext())
        {
            var recipe = await verify.Recipes.Include(item => item.Ingredients).SingleAsync(item => item.Id == recipeId);
            var ingredient = Assert.Single(recipe.Ingredients);
            Assert.Equal("Limonada actualizada", recipe.Name);
            Assert.Equal("Nueva descripción", recipe.Description);
            Assert.Equal(productId, ingredient.ProductId);
            Assert.Equal(0.5m, ingredient.QuantityPerServing);
        }

        await using (var context = _database.CreateContext())
        {
            var result = await new EfServiceSet(context, _database.Clock).Recipes.DeleteAsync(recipeId);
            Assert.True(result.Succeeded);
        }

        await using var deleted = _database.CreateContext();
        Assert.False(await deleted.Recipes.AnyAsync(recipe => recipe.Id == recipeId));
        Assert.Equal(0, await deleted.RecipeIngredients.CountAsync(ingredient =>
            EF.Property<int>(ingredient, "RecipeId") == recipeId));
        Assert.True(await deleted.Products.AnyAsync(product => product.Id == productId));
    }
}
