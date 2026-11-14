using DAS.Domain.Common;
using DAS.Domain.Enums;

namespace DAS.Domain.Entities;

//Registro del kardex: lo crea InventoryProduct y no se modifica después (init)
public class InventoryMovement : Entity
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public MovementType Type { get; init; }
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal StockBefore { get; init; }
    public decimal StockAfter { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public DateTime RecordedAt { get; init; }
}
