using DAS.Domain.Common;
using DAS.Domain.Enums;
using DAS.Domain.Exceptions;

namespace DAS.Domain.Entities;

public class InventoryProduct : Entity, IAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Presentation { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public decimal BaseUnitsPerStockUnit { get; set; } = 1;

    //Solo cambia mediante RegisterMovement o AdjustToCount, así todo cambio queda en el kardex
    public decimal Stock { get; private set; }

    public decimal MinimumStock { get; set; }
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = "Barra";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsLowStock => Stock <= MinimumStock;

    public bool HasStockFor(decimal quantity) => quantity <= Stock;

    //Convierte unidades base de receta (oz, lb...) a la unidad en que se controla la existencia
    public decimal ToStockUnits(decimal baseQuantity)
    {
        if (BaseUnitsPerStockUnit <= 0)
        {
            throw new DomainException($"{Name} no tiene una equivalencia de unidades válida.");
        }

        return Math.Round(baseQuantity / BaseUnitsPerStockUnit, 4, MidpointRounding.AwayFromZero);
    }

    public InventoryMovement RegisterMovement(MovementType type, decimal quantity, string reason, string userName, DateTime recordedAt)
    {
        if (type == MovementType.Ajuste)
        {
            throw new DomainException("Los ajustes solo se generan desde el conteo físico.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("La cantidad del movimiento debe ser mayor que cero.");
        }

        return Apply(type, type == MovementType.Entrada ? quantity : -quantity, reason, userName, recordedAt);
    }

    //Concilia el saldo teórico con lo contado; si no hay diferencia no se genera movimiento
    public InventoryMovement? AdjustToCount(decimal countedStock, string reason, string userName, DateTime recordedAt)
    {
        if (countedStock < 0)
        {
            throw new DomainException("La cantidad contada no puede ser negativa.");
        }

        var difference = countedStock - Stock;
        return difference == 0 ? null : Apply(MovementType.Ajuste, difference, reason, userName, recordedAt);
    }

    private InventoryMovement Apply(MovementType type, decimal delta, string reason, string userName, DateTime recordedAt)
    {
        if (Id <= 0)
        {
            throw new InvalidOperationException("El producto debe guardarse antes de registrar movimientos.");
        }

        var stockAfter = Stock + delta;
        if (stockAfter < 0)
        {
            throw new InsufficientStockException(Name, Stock, Unit);
        }

        var movement = new InventoryMovement
        {
            ProductId = Id,
            ProductName = Name,
            Type = type,
            Quantity = Math.Abs(delta),
            Unit = Unit,
            StockBefore = Stock,
            StockAfter = stockAfter,
            Reason = reason.Trim(),
            UserName = userName,
            RecordedAt = recordedAt
        };

        Stock = stockAfter;
        return movement;
    }
}
