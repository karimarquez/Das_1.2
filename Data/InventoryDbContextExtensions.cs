using DAS.Models.Entities;
using DAS.Models.Enums;

namespace DAS.Data;

public static class InventoryDbContextExtensions
{
    public static InventoryMovement AddMovement(
        this AppDbContext db,
        InventoryProduct product,
        MovementType type,
        decimal delta,
        string reason,
        string userName,
        DateTime recordedAt)
    {
        var stockAfter = product.Stock + delta;
        if (stockAfter < 0)
        {
            throw new InvalidOperationException($"La existencia de {product.Name} no puede quedar negativa.");
        }

        var movement = new InventoryMovement
        {
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
        db.Movements.Add(movement);
        return movement;
    }
}
