using System.ComponentModel.DataAnnotations;
using DAS.Application.DTOs;

namespace DAS.Web.Models.Api;

public sealed record DashboardResponse(
    int ProductCount,
    int LowStockCount,
    int TodayMovementCount,
    int TodayConsumptionCount,
    IReadOnlyList<ProductStockDto> Products);

public sealed record MovementRegisteredResponse(MovementDto Movement, ProductStockDto Product);

public sealed record PhysicalCountRegisteredResponse(PhysicalCountDto Count, ProductStockDto Product);

public sealed record PrepareRecipeRequest
{
    [Display(Name = "Porciones")]
    [Range(1, 1000, ErrorMessage = "Las porciones deben estar entre {1} y {2}.")]
    public int Servings { get; init; } = 1;
}
