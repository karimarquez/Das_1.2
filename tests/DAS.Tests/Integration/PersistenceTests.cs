using DAS.Application.DTOs;
using DAS.Domain.Entities;
using DAS.Domain.Enums;
using DAS.Infrastructure.Persistence;
using DAS.Infrastructure.Persistence.Repositories;
using DAS.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace DAS.Tests.Integration;

public class PersistenceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void Migraciones_SeAplicanYElModeloNoTieneCambiosPendientes()
    {
        using var context = _database.CreateContext();

        var applied = context.Database.GetAppliedMigrations().ToList();

        Assert.Contains("20261111202322_InitialCreate", applied);
        Assert.Contains("20261114163110_IdentityAndPersistentInventory", applied);
        Assert.False(context.Database.HasPendingModelChanges(), "El modelo cambió y falta una migración");
    }

    [Fact]
    public async Task UnidadDeTrabajo_ConfirmaJuntosLosCambiosDeVariosRepositorios()
    {
        await using (var context = _database.CreateContext())
        {
            var unitOfWork = new UnitOfWork(context);
            var products = new ProductRepository(context);
            var movements = new MovementRepository(context);

            await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var product = NewProduct("Ron añejo");
                products.Add(product);
                await unitOfWork.SaveChangesAsync(token);
                movements.Add(product.RegisterMovement(MovementType.Entrada, 4, "Compra", TestData.UserName, TestData.Now));
                return await unitOfWork.SaveChangesAsync(token);
            });
        }

        await using var verify = _database.CreateContext();
        var saved = await verify.Products.SingleAsync(product => product.Name == "Ron añejo");
        Assert.Equal(4, saved.Stock);
        Assert.Equal(1, await verify.Movements.CountAsync(movement => movement.ProductId == saved.Id));
    }

    [Fact]
    public async Task UnidadDeTrabajo_RevierteTodoSiLaOperacionFalla()
    {
        await using (var context = _database.CreateContext())
        {
            var unitOfWork = new UnitOfWork(context);
            var products = new ProductRepository(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync<int>(async token =>
            {
                products.Add(NewProduct("Ron añejo"));
                await unitOfWork.SaveChangesAsync(token);
                throw new InvalidOperationException("Falla simulada después de guardar");
            }));
        }

        await using var verify = _database.CreateContext();
        Assert.False(await verify.Products.AnyAsync(product => product.Name == "Ron añejo"));
    }

    [Fact]
    public async Task IndiceUnico_ImpideDosProductosConElMismoNombre()
    {
        await using var context = _database.CreateContext();
        context.Products.Add(NewProduct("Agua de coco"));
        await context.SaveChangesAsync();

        context.Products.Add(NewProduct("Agua de coco"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ProductoConMovimientos_NoSePuedeEliminarPorLaRestriccionDeLaBase()
    {
        int productId;
        await using (var context = _database.CreateContext())
        {
            var product = NewProduct("Granadina");
            context.Products.Add(product);
            await context.SaveChangesAsync();
            context.Movements.Add(product.RegisterMovement(MovementType.Entrada, 2, "Compra", TestData.UserName, TestData.Now));
            await context.SaveChangesAsync();
            productId = product.Id;
        }

        //Contexto nuevo: el movimiento no está cargado, así quien rechaza el borrado es la llave foránea
        await using var delete = _database.CreateContext();
        var repository = new ProductRepository(delete);
        repository.Remove((await repository.GetByIdAsync(productId))!);

        await Assert.ThrowsAsync<DbUpdateException>(() => delete.SaveChangesAsync());
    }

    [Fact]
    public async Task RepositorioDeProductos_ComparaNombresSinDistinguirMayusculas()
    {
        int id;
        await using (var context = _database.CreateContext())
        {
            var product = NewProduct("Triple sec");
            context.Products.Add(product);
            await context.SaveChangesAsync();
            id = product.Id;
        }

        await using var query = _database.CreateContext();
        var repository = new ProductRepository(query);
        Assert.True(await repository.NameExistsAsync("  TRIPLE SEC "));
        Assert.False(await repository.NameExistsAsync("Triple sec", excludeId: id));
    }

    [Fact]
    public async Task RepositorioDeMovimientos_FiltraPorTipoYFechasDelMasRecienteAlMasAntiguo()
    {
        await using (var context = _database.CreateContext())
        {
            var product = NewProduct("Jarabe natural");
            context.Products.Add(product);
            await context.SaveChangesAsync();
            context.Movements.AddRange(
                product.RegisterMovement(MovementType.Entrada, 10, "Compra", TestData.UserName, new DateTime(2026, 11, 1, 9, 0, 0)),
                product.RegisterMovement(MovementType.Consumo, 2, "Turno 1", TestData.UserName, new DateTime(2026, 11, 2, 21, 0, 0)),
                product.RegisterMovement(MovementType.Consumo, 3, "Turno 2", TestData.UserName, new DateTime(2026, 11, 3, 21, 0, 0)),
                product.RegisterMovement(MovementType.Consumo, 1, "Turno 3", TestData.UserName, new DateTime(2026, 11, 5, 21, 0, 0)));
            await context.SaveChangesAsync();
        }

        await using var query = _database.CreateContext();
        var found = await new MovementRepository(query).SearchAsync(
            new MovementQueryDto(Type: MovementType.Consumo, From: new DateTime(2026, 11, 2), To: new DateTime(2026, 11, 3)));

        Assert.Equal(new[] { "Turno 2", "Turno 1" }, found.Select(movement => movement.Reason));

        var repository = new MovementRepository(query);
        var page = await repository.SearchPageAsync(
            new MovementQueryDto(Type: MovementType.Consumo, From: new DateTime(2026, 11, 2), To: new DateTime(2026, 11, 3)),
            page: 2,
            pageSize: 1);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal("Turno 1", Assert.Single(page.Items).Reason);

        var groupedCounts = await repository.CountByProductAndTypeAsync(
            new DateTime(2026, 11, 2),
            new DateTime(2026, 11, 3));
        var consumptionCount = Assert.Single(groupedCounts);
        Assert.Equal("Jarabe natural", consumptionCount.ProductName);
        Assert.Equal(MovementType.Consumo, consumptionCount.Type);
        Assert.Equal(2, consumptionCount.Count);
    }

    [Fact]
    public async Task RepositorioDeRecetas_CargaLosIngredientes()
    {
        int recipeId;
        await using (var context = _database.CreateContext())
        {
            var lime = NewProduct("Lima");
            context.Products.Add(lime);
            await context.SaveChangesAsync();
            var recipe = new Recipe
            {
                Name = "Agua de lima",
                Description = "Prueba",
                Ingredients = [new RecipeIngredient { ProductId = lime.Id, QuantityPerServing = 2, BaseUnit = lime.BaseUnit }]
            };
            context.Recipes.Add(recipe);
            await context.SaveChangesAsync();
            recipeId = recipe.Id;
        }

        await using var query = _database.CreateContext();
        var loaded = await new RecipeRepository(query).GetWithIngredientsAsync(recipeId);

        Assert.NotNull(loaded);
        Assert.Equal(2, Assert.Single(loaded!.Ingredients).QuantityPerServing);
    }

    [Fact]
    public async Task Interceptor_AsignaLasFechasDeAuditoriaAlGuardar()
    {
        await using var context = _database.CreateContext();
        var product = NewProduct("Licor de café");

        context.Products.Add(product);
        await context.SaveChangesAsync();

        Assert.Equal(TestData.Now, product.CreatedAt);
        Assert.Equal(TestData.Now, product.UpdatedAt);
    }

    private static InventoryProduct NewProduct(string name) => new()
    {
        Name = name,
        Category = "Licores",
        Presentation = "Botella 750 ml",
        Unit = "botellas",
        BaseUnit = "oz",
        BaseUnitsPerStockUnit = 25.36m,
        MinimumStock = 1,
        UnitCost = 10m,
        Location = "Estante"
    };
}
