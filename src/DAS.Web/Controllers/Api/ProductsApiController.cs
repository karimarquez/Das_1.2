using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Web.Controllers.Api;

//API JSON de solo lectura para integraciones futuras (requisiciones, tablero externo)
[ApiController]
[Route("api/productos")]
[Produces("application/json")]
public class ProductsApiController(IProductQueryService productService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Existencias de los productos activos")]
    [EndpointDescription("Lista los productos activos con su existencia, unidad y mínimo, sin costos. Con bajoMinimo=true devuelve solo los productos en o bajo su mínimo.")]
    public async Task<ActionResult<IEnumerable<ProductStockDto>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery(Name = "bajoMinimo")] bool lowStockOnly = false)
    {
        var products = (await productService.GetActiveAsync(cancellationToken))
            .Where(product => !lowStockOnly || product.IsLowStock)
            .Select(product => product.ToStockDto())
            .ToList();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Existencia de un producto activo")]
    [EndpointDescription("Devuelve la existencia de un producto activo. Un producto inexistente o desactivado responde 404.")]
    [ProducesResponseType<ProductStockDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductStockDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return NotFound();
        }

        return product.ToStockDto();
    }
}
