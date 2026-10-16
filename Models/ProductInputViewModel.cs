using System.ComponentModel.DataAnnotations;

namespace DAS.Models;

public class ProductInputViewModel
{
    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = "Cervezas y sodas";

    [Required, StringLength(50)]
    public string Presentation { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Unit { get; set; } = "unidad";

    [Required, StringLength(20)]
    public string BaseUnit { get; set; } = "unidad";

    [Range(typeof(decimal), "0.0001", "1000000")]
    public decimal BaseUnitsPerStockUnit { get; set; } = 1;

    [Range(typeof(decimal), "0", "1000000")]
    public decimal Stock { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal MinimumStock { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal UnitCost { get; set; }
}