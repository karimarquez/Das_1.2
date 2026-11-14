using DAS.Mappings;
using DAS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/inventario/productos")]
public sealed class ApiProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetProducts([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Ok(productService.SearchStock(search, page, pageSize));
}
