using DAS.Application.Services;
using DAS.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace DAS.Tests.Fakes;

//Arma los servicios reales sobre repositorios en memoria: así se prueban las reglas sin base de datos
public sealed class TestInventory
{
    public TestInventory()
    {
        UnitOfWork.Track(Products);
        UnitOfWork.Track(Movements);
        UnitOfWork.Track(Recipes);
        UnitOfWork.Track(Counts);
    }

    public InMemoryProductRepository Products { get; } = new();
    public InMemoryMovementRepository Movements { get; } = new();
    public InMemoryRecipeRepository Recipes { get; } = new();
    public InMemoryPhysicalCountRepository Counts { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public FixedTimeProvider Clock { get; } = new(TestData.Now);
    public FakeCurrentUser User { get; } = new();

    public ProductService ProductService() =>
        new(Products, Movements, UnitOfWork, User, Clock, NullLogger<ProductService>.Instance);

    public MovementService MovementService() =>
        new(Products, Movements, UnitOfWork, User, Clock, NullLogger<MovementService>.Instance);

    public PhysicalCountService PhysicalCountService() =>
        new(Products, Movements, Counts, UnitOfWork, User, Clock, NullLogger<PhysicalCountService>.Instance);

    public RecipeService RecipeService() =>
        new(Recipes, Products, Movements, UnitOfWork, User, Clock, NullLogger<RecipeService>.Instance);

    public ReportService ReportService() => new(Products, Movements);

    public InventoryProduct AddProduct(
        string name,
        decimal stock = 0,
        decimal minimum = 0,
        string unit = "botellas",
        string baseUnit = "botellas",
        decimal equivalence = 1,
        decimal cost = 1m,
        bool active = true)
    {
        var product = TestData.Product(Products.NextId(), name, stock, minimum, unit, baseUnit, equivalence, cost, active);
        Products.Seed(product);
        return product;
    }

    public Recipe AddRecipe(string name, params (InventoryProduct Product, decimal Quantity)[] ingredients)
    {
        var recipe = new Recipe
        {
            Name = name,
            Description = $"Receta de prueba: {name}",
            Ingredients = ingredients
                .Select(item => new RecipeIngredient { ProductId = item.Product.Id, QuantityPerServing = item.Quantity, BaseUnit = item.Product.BaseUnit })
                .ToList()
        };
        Recipes.Seed(recipe);
        return recipe;
    }
}
