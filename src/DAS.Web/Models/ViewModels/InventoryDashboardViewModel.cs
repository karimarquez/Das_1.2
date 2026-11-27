using DAS.Application.DTOs;
using DAS.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Web.Models.ViewModels;

public class InventoryDashboardViewModel
{
    public IReadOnlyList<ProductDto> Products { get; init; } = [];
    public IReadOnlyList<MovementDto> RecentMovements { get; init; } = [];
    public int ProductCount { get; init; }
    public int LowStockCount { get; init; }
    public int TodayMovementCount { get; init; }
    public int TodayConsumptionCount { get; init; }

    public MovementInputViewModel Movement { get; } = new();
    public PhysicalCountInputViewModel Count { get; } = new();

    public IEnumerable<SelectListItem> MovementTypeOptions =>
        new[] { MovementType.Entrada, MovementType.Consumo, MovementType.Merma }
            .Select(type => new SelectListItem(type.ToString(), type.ToString()));

    public IEnumerable<SelectListItem> ProductOptions =>
        Products.Select(product => new SelectListItem($"{product.Name} · {product.Stock:0.##} {product.Unit}", product.Id.ToString()));
}
