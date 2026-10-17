using DAS.Configuration;
using DAS.Extensions;
using DAS.Models.DTOs;
using DAS.Models.Enums;
using DAS.Models.ViewModels;
using DAS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DAS.Controllers;

[Route("inventario")]
public class InventoryController(
    IProductService productService,
    IMovementService movementService,
    IPhysicalCountService countService,
    IOptions<InventoryOptions> options) : Controller
{
    [HttpGet("~/")]
    [HttpGet("")]
    public IActionResult Index()
    {
        var products = productService.GetActive();
        var today = movementService.Search(new MovementQueryDto(From: DateTime.Today, To: DateTime.Today));

        var model = new InventoryDashboardViewModel
        {
            Products = products,
            RecentMovements = movementService.Search(new MovementQueryDto(Take: options.Value.RecentMovementsCount)),
            ProductCount = products.Count,
            LowStockCount = products.Count(product => product.IsLowStock),
            TodayMovementCount = today.Count,
            TodayConsumptionCount = today.Count(movement => movement.Type == MovementType.Consumo)
        };

        return View(model);
    }

    [HttpGet("conteo")]
    public IActionResult Count()
    {
        return View(BuildCountSheet());
    }

    [HttpPost("conteo")]
    [ValidateAntiForgeryToken]
    public IActionResult Count(PhysicalCountSheetViewModel sheet)
    {
        if (ModelState.IsValid)
        {
            var lines = sheet.Lines
                .Where(line => line.CountedStock.HasValue)
                .Select(line => new CountLineDto(line.ProductId, line.CountedStock!.Value))
                .ToList();

            var result = countService.Register(lines);
            if (result.Succeeded)
            {
                TempData.Success("Conteo registrado. Revisa las diferencias encontradas.");
                return RedirectToAction(nameof(CountResult), new { id = result.Value!.Id });
            }

            ModelState.AddServiceError(result);
        }

        return View(BuildCountSheet(sheet));
    }

    [HttpGet("conteo/{id:int}")]
    public IActionResult CountResult(int id)
    {
        var count = countService.GetById(id);
        if (count is null)
        {
            return NotFound();
        }

        return View(new PhysicalCountResultViewModel(count));
    }

    //Conteo rápido de un producto desde el tablero; [Bind] limita los campos que se aceptan del formulario
    [HttpPost("conteo/producto")]
    [ValidateAntiForgeryToken]
    public IActionResult RegisterCount([Bind(nameof(PhysicalCountInputViewModel.ProductId), nameof(PhysicalCountInputViewModel.ActualStock))] PhysicalCountInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData.Error(ModelState.FirstError("No fue posible registrar el conteo físico."));
            return RedirectToAction(nameof(Index));
        }

        var result = countService.Register([new CountLineDto(input.ProductId, input.ActualStock)]);
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

    private PhysicalCountSheetViewModel BuildCountSheet(PhysicalCountSheetViewModel? posted = null)
    {
        var typed = new Dictionary<int, decimal?>();
        foreach (var line in posted?.Lines ?? [])
        {
            typed.TryAdd(line.ProductId, line.CountedStock);
        }

        return new PhysicalCountSheetViewModel
        {
            Lines = productService.GetActive()
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
            RecentCounts = countService.GetRecent(5)
        };
    }
}
