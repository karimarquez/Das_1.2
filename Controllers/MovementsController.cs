using DAS.Extensions;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.ViewModels;
using DAS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Controllers;

[Route("movimientos")]
public class MovementsController(IMovementService movementService, IProductService productService) : Controller
{
    [HttpGet("")]
    public IActionResult Index([FromQuery] MovementFilterViewModel filter)
    {
        IReadOnlyList<MovementDto> movements = ModelState.IsValid ? movementService.Search(filter.ToQuery()) : [];

        var model = new MovementListViewModel
        {
            Filter = filter,
            Movements = movements,
            ProductOptions = productService.GetAll()
                .Select(product => new SelectListItem(product.IsActive ? product.Name : $"{product.Name} (inactivo)", product.Id.ToString()))
                .ToList()
        };

        return View(model);
    }

    [HttpPost("registrar")]
    [ValidateAntiForgeryToken]
    public IActionResult Register([FromForm] MovementInputViewModel input, [FromForm] string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            TempData.Error(ModelState.FirstError("Revisa el producto, la cantidad y el motivo."));
        }
        else
        {
            var result = movementService.Register(input.ToDto());
            if (result.Succeeded)
            {
                var movement = result.Value!;
                TempData.Success($"Movimiento registrado: {movement.Type} de {movement.Quantity:0.##} {movement.Unit} · {movement.ProductName}.");
            }
            else
            {
                TempData.Error(result.Error!);
            }
        }

        //Solo se regresa a direcciones locales para evitar redirecciones abiertas
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(InventoryController.Index), "Inventory");
    }
}
