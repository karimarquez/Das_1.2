using DAS.Application.DTOs;
using DAS.Domain.Entities;

namespace DAS.Application.Mappings;

public static class ProductMappings
{
    public static ProductDto ToDto(this InventoryProduct product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Category = product.Category,
        Presentation = product.Presentation,
        Unit = product.Unit,
        BaseUnit = product.BaseUnit,
        BaseUnitsPerStockUnit = product.BaseUnitsPerStockUnit,
        Stock = product.Stock,
        MinimumStock = product.MinimumStock,
        UnitCost = product.UnitCost,
        Location = product.Location,
        IsActive = product.IsActive,
        IsLowStock = product.IsLowStock,
        UpdatedAt = product.UpdatedAt
    };

    public static ProductStockDto ToStockDto(this InventoryProduct product) =>
        new(product.Id, product.Name, product.Category, product.Stock, product.Unit, product.MinimumStock, product.IsLowStock);

    public static ProductStockDto ToStockDto(this ProductDto product) =>
        new(product.Id, product.Name, product.Category, product.Stock, product.Unit, product.MinimumStock, product.IsLowStock);
}
