using DAS.Application.DTOs;
using DAS.Application.Security;
using DAS.Application.Services.Interfaces;
using DAS.Web.Extensions;
using DAS.Web.Mappings;
using DAS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Web.Controllers;

[Route("movimientos")]
public class MovementsController(IMovementService movementService, IProductQueryService productService) : Controller
{
    private const int PageSize = 25;

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] MovementFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = ModelState.IsValid
            ? await movementService.SearchPageAsync(filter.ToQuery(), filter.Page, PageSize, cancellationToken)
            : new MovementPageDto([], 1, PageSize, 0);
        filter.Page = page.Page;
        var products = await productService.GetAllAsync(cancellationToken);

        var model = new MovementListViewModel
        {
            Filter = filter,
            Movements = page.Items,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            ProductOptions = products
                .Select(product => new SelectListItem(product.IsActive ? product.Name : $"{product.Name} (inactivo)", product.Id.ToString()))
                .ToList()
        };

        return View(model);
    }

    [HttpPost("registrar")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] MovementInputViewModel input, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData.Error(ModelState.FirstError("Revisa el producto, la cantidad y el motivo."));
        }
        else
        {
            var result = await movementService.RegisterAsync(input.ToDto(), cancellationToken);
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
