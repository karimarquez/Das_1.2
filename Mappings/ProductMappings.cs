using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.ViewModels;

namespace DAS.Mappings;

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

    public static ProductStockDto ToStockDto(this ProductDto product) =>
        new(product.Id, product.Name, product.Category, product.Stock, product.Unit, product.MinimumStock, product.IsLowStock);

    public static SaveProductDto ToDto(this ProductFormViewModel form) => new()
    {
        Name = form.Name.Trim(),
        Category = form.Category.Trim(),
        Presentation = form.Presentation.Trim(),
        Unit = form.Unit.Trim(),
        BaseUnit = form.BaseUnit.Trim(),
        BaseUnitsPerStockUnit = form.BaseUnitsPerStockUnit,
        MinimumStock = form.MinimumStock,
        UnitCost = form.UnitCost,
        Location = form.Location.Trim(),
        InitialStock = form.IsNew ? form.InitialStock : 0
    };

    public static ProductFormViewModel ToFormViewModel(this ProductDto product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Category = product.Category,
        Presentation = product.Presentation,
        Unit = product.Unit,
        BaseUnit = product.BaseUnit,
        BaseUnitsPerStockUnit = product.BaseUnitsPerStockUnit,
        MinimumStock = product.MinimumStock,
        UnitCost = product.UnitCost,
        Location = product.Location,
        CurrentStock = product.Stock
    };
}
