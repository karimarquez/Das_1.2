using DAS.Application.DTOs;
using DAS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Web.Controllers;

/// <summary>
/// API REST de productos del inventario. Requiere un token JWT (POST api/auth/token).
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/inventario/productos")]
public sealed class ApiProductsController(IProductService productService) : ControllerBase
{
    /// <summary>Lista productos con existencias, con búsqueda y paginación.</summary>
    /// <response code="200">Lista de productos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductStockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25) =>
        Ok(await productService.SearchStockAsync(search, page, pageSize, cancellationToken));

    /// <summary>Obtiene un producto por su identificador.</summary>
    /// <response code="200">El producto solicitado.</response>
    /// <response code="404">No existe un producto con ese id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound(new { error = "Producto no encontrado." }) : Ok(product);
    }

    /// <summary>Crea un producto nuevo.</summary>
    /// <response code="201">Producto creado.</response>
    /// <response code="400">Datos inválidos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] SaveProductDto input, CancellationToken cancellationToken)
    {
        var result = await productService.CreateAsync(input, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error, field = result.Field });
        }

        return CreatedAtAction(nameof(GetProduct), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Actualiza los datos de un producto existente.</summary>
    /// <response code="200">Producto actualizado.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="404">No existe un producto con ese id.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] SaveProductDto input, CancellationToken cancellationToken)
    {
        if (await productService.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotFound(new { error = "Producto no encontrado." });
        }

        var result = await productService.UpdateAsync(id, input, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, field = result.Field });
    }

    /// <summary>Desactiva un producto (borrado lógico: conserva el historial).</summary>
    /// <response code="204">Producto desactivado.</response>
    /// <response code="404">No existe un producto con ese id.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        if (await productService.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotFound(new { error = "Producto no encontrado." });
        }

        var result = await productService.SetActiveAsync(id, false, cancellationToken);
        return result.Succeeded
            ? NoContent()
            : BadRequest(new { error = result.Error, field = result.Field });
    }
}