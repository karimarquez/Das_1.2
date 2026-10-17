namespace DAS.Models.Entities;

public class InventoryProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Presentation { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public decimal BaseUnitsPerStockUnit { get; set; } = 1;
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = "Barra";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsLowStock => Stock <= MinimumStock;
}
