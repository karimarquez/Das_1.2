using DAS.Application.Mappings;
using DAS.Application.Security;
using DAS.Application.Services.Interfaces;
using DAS.Web.Extensions;
using DAS.Web.Mappings;
using DAS.Web.Models.Api;
using DAS.Web.Models.ViewModels;
using DAS.Web.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Web.Controllers.Api;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[Route("api/movimientos")]
[Tags(ApiTags.Movements)]
public sealed class MovementsApiController(IMovementService movementService, IProductQueryService productService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = AppRoles.Operadores)]
    [Consumes("application/json")]
    [EndpointSummary("Registra una entrada, consumo o merma")]
    [EndpointDescription("Valida los datos, actualiza la existencia en una sola transacción y guarda la cuenta que registró el movimiento. Consumo y merma no pueden superar la existencia y los ajustes solo se generan con el conteo físico. Responde con el movimiento y la existencia actualizada del producto para refrescar la pantalla sin recargarla.")]
    [ProducesResponseType<MovementRegisteredResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<MovementRegisteredResponse>> Register([FromBody] MovementInputViewModel input, CancellationToken cancellationToken)
    {
        var result = await movementService.RegisterAsync(input.ToDto(), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddServiceError(result);
            return ValidationProblem(ModelState);
        }

        var movement = result.Value!;
        var product = await productService.GetByIdAsync(movement.ProductId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new MovementRegisteredResponse(movement, product!.ToStockDto()));
    }
}
