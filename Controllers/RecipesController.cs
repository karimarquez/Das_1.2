using DAS.Extensions;
using DAS.Mappings;
using DAS.Models.ViewModels;
using DAS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Controllers;

[Route("recetas")]
public class RecipesController(IRecipeService recipeService, IProductService productService) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new RecipesViewModel { Recipes = recipeService.GetAll() });
    }

    [HttpPost("{recipeId:int}/preparar")]
    [ValidateAntiForgeryToken]
    public IActionResult Prepare(RecipeConsumptionInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData.Error(ModelState.FirstError("Indica la receta y el número de porciones."));
            return RedirectToAction(nameof(Index));
        }

        var result = recipeService.Prepare(input.ToDto());
        if (!result.Succeeded)
        {
            TempData.Error(result.Error!);
            return RedirectToAction(nameof(Index));
        }

        var preparation = result.Value!;
        var detail = string.Join(", ", preparation.Deductions.Select(item => $"{item.ProductName} {item.Quantity:0.####} {item.Unit}"));
        TempData.Success($"Preparación registrada: {preparation.Servings} porción(es) de {preparation.RecipeName}. Se descontó {detail}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("nueva")]
    public IActionResult Create()
    {
        return View(PrepareForm(new RecipeFormViewModel()));
    }

    [HttpPost("nueva")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(RecipeFormViewModel input)
    {
        if (ModelState.IsValid)
        {
            var result = recipeService.Create(input.ToDto());
            if (result.Succeeded)
            {
                TempData.Success($"La receta {result.Value!.Name} quedó registrada.");
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddServiceError(result);
        }

        return View(PrepareForm(input));
    }

    private RecipeFormViewModel PrepareForm(RecipeFormViewModel form)
    {
        while (form.Ingredients.Count < RecipeFormViewModel.MaxIngredients)
        {
            form.Ingredients.Add(new RecipeIngredientInputViewModel());
        }

        form.ProductOptions = productService.GetActive()
            .Select(product => new SelectListItem($"{product.Name} ({product.BaseUnit})", product.Id.ToString()))
            .ToList();

        return form;
    }
}
