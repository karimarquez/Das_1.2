using DAS.Models;
using DAS.Services;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Controllers;

public class InventoryController(InventoryService inventoryService) : Controller
{
    public IActionResult Index()
    {
        return View(inventoryService.GetDashboard());
    }

    public IActionResult Movements()
    {
        return View(inventoryService.GetMovements());
    }

    public IActionResult Reports(string period = "month")
    {
        var isDaily = period == "day";
        var periodStart = isDaily ? DateTime.Today : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var movements = inventoryService.GetMovements(periodStart, DateTime.Today);
        return View(new ReportsViewModel
        {
            Movements = movements,
            EntryCount = movements.Count(movement => movement.Type == "Entrada"),
            ConsumptionCount = movements.Count(movement => movement.Type == "Consumo"),
            WasteCount = movements.Count(movement => movement.Type == "Merma"),
            ProductsConsumedCount = movements.Where(movement => movement.Type == "Consumo").Select(movement => movement.ProductId).Distinct().Count(),
            PeriodLabel = isDaily
                ? DateTime.Today.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-SV"))
                : DateTime.Today.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-SV")),
            IsDaily = isDaily
        });
    }

    public IActionResult Recipes()
    {
        return View(new RecipesViewModel { Recipes = inventoryService.GetRecipes() });
    }

    public IActionResult CreateProduct()
    {
        return View(new ProductInputViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateProduct(ProductInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        inventoryService.AddProduct(input);
        TempData["Success"] = $"{input.Name} se agregó al catálogo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RecordMovement(MovementInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revisa el producto, la cantidad y el motivo.";
            return RedirectToAction(nameof(Index));
        }

        if (!inventoryService.RecordMovement(input, out var error))
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Movimiento registrado y existencias actualizadas.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConsumeRecipe(RecipeConsumptionInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Indica la receta y el número de porciones.";
            return RedirectToAction(nameof(Recipes));
        }

        if (!inventoryService.ConsumeRecipe(input, out var error))
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Recipes));
        }

        TempData["Success"] = "Preparación registrada; los ingredientes se descontaron del inventario.";
        return RedirectToAction(nameof(Recipes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RegisterCount(PhysicalCountInputViewModel input)
    {
        if (!ModelState.IsValid || !inventoryService.RegisterCount(input))
        {
            TempData["Error"] = "No fue posible registrar el conteo físico.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Conteo físico comparado con el saldo teórico.";
        return RedirectToAction(nameof(Index));
    }
}