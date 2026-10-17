using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Controllers.Api;

//API JSON de solo lectura para integraciones futuras (requisiciones, tablero externo)
[ApiController]
[Route("api/productos")]
[Produces("application/json")]
public class ProductsApiController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<ProductStockDto>> GetAll([FromQuery(Name = "bajoMinimo")] bool lowStockOnly = false)
    {
        var products = productService.GetActive()
            .Where(product => !lowStockOnly || product.IsLowStock)
            .Select(product => product.ToStockDto())
            .ToList();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public ActionResult<ProductStockDto> GetById(int id)
    {
        var product = productService.GetById(id);
        if (product is null || !product.IsActive)
        {
            return NotFound();
        }

        return product.ToStockDto();
    }
}
