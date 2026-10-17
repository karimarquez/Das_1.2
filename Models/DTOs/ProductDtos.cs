namespace DAS.Models.DTOs;

public sealed record ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Presentation { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string BaseUnit { get; init; } = string.Empty;
    public decimal BaseUnitsPerStockUnit { get; init; }
    public decimal Stock { get; init; }
    public decimal MinimumStock { get; init; }
    public decimal UnitCost { get; init; }
    public string Location { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsLowStock { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record SaveProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Presentation { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string BaseUnit { get; init; } = string.Empty;
    public decimal BaseUnitsPerStockUnit { get; init; } = 1;
    public decimal MinimumStock { get; init; }
    public decimal UnitCost { get; init; }
    public string Location { get; init; } = string.Empty;
    public decimal InitialStock { get; init; }
}

//Versión reducida para la API: no expone costos
public sealed record ProductStockDto(int Id, string Name, string Category, decimal Stock, string Unit, decimal MinimumStock, bool IsLowStock);
