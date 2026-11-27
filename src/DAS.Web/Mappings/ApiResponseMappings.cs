using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Web.Models.Api;

namespace DAS.Web.Mappings;

public static class ApiResponseMappings
{
    public static DashboardResponse ToResponse(this DashboardDto dashboard) => new(
        dashboard.ProductCount,
        dashboard.LowStockCount,
        dashboard.TodayMovementCount,
        dashboard.TodayConsumptionCount,
        dashboard.Products.Select(product => product.ToStockDto()).ToList());
}
