using DAS.Models.DTOs;

namespace DAS.Models.ViewModels;

public class ProductDetailsViewModel
{
    public ProductDetailsViewModel(ProductDto product, IReadOnlyList<MovementDto> movements)
    {
        Product = product;
        Movements = movements;
    }

    public ProductDto Product { get; }
    public IReadOnlyList<MovementDto> Movements { get; }

    public decimal StockValue => Product.Stock * Product.UnitCost;
    public decimal StockInBaseUnits => Product.Stock * Product.BaseUnitsPerStockUnit;
}
