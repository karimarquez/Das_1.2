using DAS.Models.Enums;

namespace DAS.Models.Entities;

public class InventoryMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public MovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
