using DAS.Application.DTOs;
using DAS.Application.Security;
using DAS.Application.Services.Interfaces;
using DAS.Web.Configuration;
using DAS.Web.Extensions;
using DAS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DAS.Web.Controllers;

[Route("inventario")]
public class InventoryController(
    IProductQueryService productService,
    IMovementService movementService,
    IPhysicalCountService countService,
    IDashboardService dashboardService,
    IOptions<InventoryOptions> options) : Controller
{
    [HttpGet("~/")]
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetAsync(options.Value.RecentMovementsCount, cancellationToken);

        var model = new InventoryDashboardViewModel
        {
            Products = dashboard.Products,
            RecentMovements = dashboard.RecentMovements,
            ProductCount = dashboard.ProductCount,
            LowStockCount = dashboard.LowStockCount,
            TodayMovementCount = dashboard.TodayMovementCount,
            TodayConsumptionCount = dashboard.TodayConsumptionCount
        };

        return View(model);
    }

    [HttpGet("actividad")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> RecentActivity(CancellationToken cancellationToken)
    {
        var recent = await movementService.SearchAsync(new MovementQueryDto(Take: options.Value.RecentMovementsCount), cancellationToken);
        return PartialView("_RecentActivity", recent);
    }

    [HttpGet("conteo")]
    [Authorize(Roles = AppRoles.Operadores)]
    public async Task<IActionResult> Count(CancellationToken cancellationToken)
    {
        return View(await BuildCountSheetAsync(null, cancellationToken));
    }

    [HttpPost("conteo")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Count(PhysicalCountSheetViewModel sheet, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var lines = sheet.Lines
                .Where(line => line.CountedStock.HasValue)
                .Select(line => new CountLineDto(line.ProductId, line.CountedStock!.Value))
                .ToList();

            var result = await countService.RegisterAsync(lines, cancellationToken);
            if (result.Succeeded)
            {
                TempData.Success("Conteo registrado. Revisa las diferencias encontradas.");
                return RedirectToAction(nameof(CountResult), new { id = result.Value!.Id });
            }

            ModelState.AddServiceError(result);
        }

        return View(await BuildCountSheetAsync(sheet, cancellationToken));
    }

    [HttpGet("conteo/{id:int}")]
    public async Task<IActionResult> CountResult(int id, CancellationToken cancellationToken)
    {
        var count = await countService.GetByIdAsync(id, cancellationToken);
        if (count is null)
        {
            return NotFound();
        }

        return View(new PhysicalCountResultViewModel(count));
    }

    //Conteo rápido de un producto desde el tablero; [Bind] limita los campos que se aceptan del formulario
    [HttpPost("conteo/producto")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterCount(
        [Bind(nameof(PhysicalCountInputViewModel.ProductId), nameof(PhysicalCountInputViewModel.ActualStock))] PhysicalCountInputViewModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData.Error(ModelState.FirstError("No fue posible registrar el conteo físico."));
            return RedirectToAction(nameof(Index));
        }

        var result = await countService.RegisterAsync([new CountLineDto(input.ProductId, input.ActualStock)], cancellationToken);
        if (!result.Succeeded)
        {
            TempData.Error(result.Error!);
            return RedirectToAction(nameof(Index));
        }

        var line = result.Value!.Lines[0];
        TempData.Success(line.Difference == 0
            ? $"Conteo de {line.ProductName} sin diferencias."
            : $"Conteo de {line.ProductName}: {(line.Difference > 0 ? "sobrante" : "faltante")} de {Math.Abs(line.Difference):0.##} {line.Unit}. Se registró el ajuste.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<PhysicalCountSheetViewModel> BuildCountSheetAsync(PhysicalCountSheetViewModel? posted, CancellationToken cancellationToken)
    {
        var typed = new Dictionary<int, decimal?>();
        foreach (var line in posted?.Lines ?? [])
        {
            typed.TryAdd(line.ProductId, line.CountedStock);
        }

        var products = await productService.GetActiveAsync(cancellationToken);
        return new PhysicalCountSheetViewModel
        {
            Lines = products
                .Select(product => new PhysicalCountLineViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Presentation = product.Presentation,
                    Category = product.Category,
                    Location = product.Location,
                    Unit = product.Unit,
                    CountedStock = typed.GetValueOrDefault(product.Id)
                })
                .ToList(),
            RecentCounts = await countService.GetRecentAsync(5, cancellationToken)
        };
    }
}
