using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Application.Security;
using DAS.Application.Services.Interfaces;
using DAS.Web.Extensions;
using DAS.Web.Models.Api;
using DAS.Web.Models.ViewModels;
using DAS.Web.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Web.Controllers.Api;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[Route("api/conteos")]
[Tags(ApiTags.PhysicalCounts)]
public sealed class PhysicalCountsApiController(IPhysicalCountService countService, IProductQueryService productService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [EndpointSummary("Resultado de un conteo físico")]
    [EndpointDescription("Devuelve el saldo teórico, la cantidad contada, la diferencia y su costo para cada producto del conteo.")]
    [ProducesResponseType<PhysicalCountDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PhysicalCountDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var count = await countService.GetByIdAsync(id, cancellationToken);
        return count is null ? NotFound() : Ok(count);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Operadores)]
    [Consumes("application/json")]
    [EndpointSummary("Registra el conteo físico de un producto")]
    [EndpointDescription("Compara la cantidad contada con el saldo teórico y, si hay diferencia, registra un ajuste de faltante o sobrante que cita el número de conteo. Responde con el conteo y la existencia conciliada del producto.")]
    [ProducesResponseType<PhysicalCountRegisteredResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PhysicalCountRegisteredResponse>> Register([FromBody] PhysicalCountInputViewModel input, CancellationToken cancellationToken)
    {
        var result = await countService.RegisterAsync([new CountLineDto(input.ProductId, input.ActualStock)], cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddServiceError(result);
            return ValidationProblem(ModelState);
        }

        var count = result.Value!;
        var product = await productService.GetByIdAsync(input.ProductId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = count.Id }, new PhysicalCountRegisteredResponse(count, product!.ToStockDto()));
    }
}
