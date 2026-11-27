using DAS.Application.Services.Interfaces;
using DAS.Web.Configuration;
using DAS.Web.Mappings;
using DAS.Web.Models.Api;
using DAS.Web.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DAS.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/tablero")]
[Tags(ApiTags.Dashboard)]
public sealed class DashboardApiController(IDashboardService dashboardService, IOptions<InventoryOptions> options) : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [EndpointSummary("Indicadores y existencias del tablero")]
    [EndpointDescription("Devuelve los mismos datos que muestra la página de inventario: productos activos, productos en o bajo su mínimo, movimientos y consumos del día y la existencia de cada producto activo. El tablero lo consulta con Fetch para actualizarse sin recargar.")]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetAsync(options.Value.RecentMovementsCount, cancellationToken);
        return dashboard.ToResponse();
    }
}
