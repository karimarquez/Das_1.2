using DAS.Application.DTOs;
using DAS.Application.Security;
using DAS.Application.Services.Interfaces;
using DAS.Web.Configuration;
using DAS.Web.Extensions;
using DAS.Web.Mappings;
using DAS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DAS.Web.Controllers;

[Route("productos")]
public class ProductsController(
    IProductService productService,
    IMovementService movementService,
    IOptions<InventoryOptions> options) : Controller
{
    private const string FormView = "Form";

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var movements = await movementService.SearchAsync(new MovementQueryDto(ProductId: id, Take: options.Value.ProductHistoryCount), cancellationToken);
        return View(new ProductDetailsViewModel(product, movements));
    }

    [HttpGet("nuevo")]
    [Authorize(Roles = AppRoles.Operadores)]
    public IActionResult Create()
    {
        return View(FormView, new ProductFormViewModel());
    }

    [HttpPost("nuevo")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(FormView, input);
        }

        var result = await productService.CreateAsync(input.ToDto(), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddServiceError(result);
            return View(FormView, input);
        }

        TempData.Success($"{result.Value!.Name} se agregó al catálogo.");
        return RedirectToAction(nameof(InventoryController.Index), "Inventory");
    }

    [HttpGet("{id:int}/editar")]
    [Authorize(Roles = AppRoles.Operadores)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return View(FormView, product.ToFormViewModel());
    }

    [HttpPost("{id:int}/editar")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, ProductFormViewModel input, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        input.Id = id;
        input.CurrentStock = product.Stock;
        if (!ModelState.IsValid)
        {
            return View(FormView, input);
        }

        var result = await productService.UpdateAsync(id, input.ToDto(), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddServiceError(result);
            return View(FormView, input);
        }

        TempData.Success("Los datos del producto se actualizaron.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/estado")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var result = await productService.SetActiveAsync(id, !product.IsActive, cancellationToken);
        if (!result.Succeeded)
        {
            TempData.Error(result.Error!);
        }
        else
        {
            TempData.Success(result.Value!.IsActive
                ? $"{product.Name} volvió a estar disponible en el inventario."
                : $"{product.Name} se desactivó; su historial se conserva.");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    //Validación remota del nombre ([Remote] en ProductFormViewModel): true si está disponible
    [AcceptVerbs("GET", "POST", Route = "validar-nombre")]
    public async Task<IActionResult> CheckName(string name, int id, CancellationToken cancellationToken)
    {
        var taken = !string.IsNullOrWhiteSpace(name) && await productService.NameExistsAsync(name, id == 0 ? null : id, cancellationToken);
        return taken ? Json("Ya existe un producto con ese nombre.") : Json(true);
    }
}
