using DAS.Models.Entities;
using DAS.Models.Enums;

namespace DAS.Data;

//Almacenamiento temporal en memoria; en la fase de persistencia se reemplaza por un DbContext de EF Core
public sealed class InMemoryInventoryStore
{
    private int _lastProductId;
    private int _lastMovementId;
    private int _lastRecipeId;
    private int _lastCountId;

    public InMemoryInventoryStore()
    {
        InventorySeedData.Load(this);
    }

    public object SyncRoot { get; } = new();
    public List<InventoryProduct> Products { get; } = [];
    public List<InventoryMovement> Movements { get; } = [];
    public List<Recipe> Recipes { get; } = [];
    public List<PhysicalCount> PhysicalCounts { get; } = [];

    public InventoryProduct AddProduct(InventoryProduct product)
    {
        product.Id = ++_lastProductId;
        Products.Add(product);
        return product;
    }

    public Recipe AddRecipe(Recipe recipe)
    {
        recipe.Id = ++_lastRecipeId;
        Recipes.Add(recipe);
        return recipe;
    }

    public PhysicalCount AddPhysicalCount(PhysicalCount count)
    {
        count.Id = ++_lastCountId;
        PhysicalCounts.Add(count);
        return count;
    }

    //Único punto que modifica existencias: todo cambio queda registrado como movimiento
    public InventoryMovement ApplyMovement(InventoryProduct product, MovementType type, decimal delta, string reason, string userName, DateTime recordedAt)
    {
        var stockAfter = product.Stock + delta;
        if (stockAfter < 0)
        {
            throw new InvalidOperationException($"La existencia de {product.Name} no puede quedar negativa.");
        }

        var movement = new InventoryMovement
        {
            Id = ++_lastMovementId,
            ProductId = product.Id,
            ProductName = product.Name,
            Type = type,
            Quantity = Math.Abs(delta),
            Unit = product.Unit,
            StockBefore = product.Stock,
            StockAfter = stockAfter,
            Reason = reason.Trim(),
            UserName = userName,
            RecordedAt = recordedAt
        };

        product.Stock = stockAfter;
        product.UpdatedAt = DateTime.Now;
        Movements.Add(movement);
        return movement;
    }
}
