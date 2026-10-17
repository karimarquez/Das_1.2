using DAS.Configuration;
using DAS.Extensions;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.ViewModels;
using DAS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DAS.Controllers;

[Route("productos")]
public class ProductsController(
    IProductService productService,
    IMovementService movementService,
    IOptions<InventoryOptions> options) : Controller
{
    private const string FormView = "Form";

    [HttpGet("{id:int}")]
    public IActionResult Details(int id)
    {
        var product = productService.GetById(id);
        if (product is null)
        {
            return NotFound();
        }

        var movements = movementService.Search(new MovementQueryDto(ProductId: id, Take: options.Value.ProductHistoryCount));
        return View(new ProductDetailsViewModel(product, movements));
    }

    [HttpGet("nuevo")]
    public IActionResult Create()
    {
        return View(FormView, new ProductFormViewModel());
    }

    [HttpPost("nuevo")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ProductFormViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(FormView, input);
        }

        var result = productService.Create(input.ToDto());
        if (!result.Succeeded)
        {
            ModelState.AddServiceError(result);
            return View(FormView, input);
        }

        TempData.Success($"{result.Value!.Name} se agregó al catálogo.");
        return RedirectToAction(nameof(InventoryController.Index), "Inventory");
    }

    [HttpGet("{id:int}/editar")]
    public IActionResult Edit(int id)
    {
        var product = productService.GetById(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(FormView, product.ToFormViewModel());
    }

    [HttpPost("{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public IActionResult Edit([FromRoute] int id, ProductFormViewModel input)
    {
        var product = productService.GetById(id);
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

        var result = productService.Update(id, input.ToDto());
        if (!result.Succeeded)
        {
            ModelState.AddServiceError(result);
            return View(FormView, input);
        }

        TempData.Success("Los datos del producto se actualizaron.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/estado")]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActive(int id)
    {
        var product = productService.GetById(id);
        if (product is null)
        {
            return NotFound();
        }

        var result = productService.SetActive(id, !product.IsActive);
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
    public IActionResult CheckName(string name, int id)
    {
        var taken = !string.IsNullOrWhiteSpace(name) && productService.NameExists(name, id == 0 ? null : id);
        return taken ? Json("Ya existe un producto con ese nombre.") : Json(true);
    }
}
