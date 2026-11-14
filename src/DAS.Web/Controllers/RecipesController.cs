using DAS.Application.Security;
using DAS.Application.Services.Interfaces;
using DAS.Web.Extensions;
using DAS.Web.Mappings;
using DAS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Web.Controllers;

[Route("recetas")]
public class RecipesController(IRecipeService recipeService, IProductQueryService productService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new RecipesViewModel { Recipes = await recipeService.GetAllAsync(cancellationToken) });
    }

    [HttpPost("{recipeId:int}/preparar")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Prepare(RecipeConsumptionInputViewModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData.Error(ModelState.FirstError("Indica la receta y el número de porciones."));
            return RedirectToAction(nameof(Index));
        }

        var result = await recipeService.PrepareAsync(input.ToDto(), cancellationToken);
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
    [Authorize(Roles = AppRoles.Operadores)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await PrepareFormAsync(new RecipeFormViewModel(), cancellationToken));
    }

    [HttpPost("nueva")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecipeFormViewModel input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await recipeService.CreateAsync(input.ToDto(), cancellationToken);
            if (result.Succeeded)
            {
                TempData.Success($"La receta {result.Value!.Name} quedó registrada.");
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddServiceError(result);
        }

        return View(await PrepareFormAsync(input, cancellationToken));
    }

    private async Task<RecipeFormViewModel> PrepareFormAsync(RecipeFormViewModel form, CancellationToken cancellationToken)
    {
        while (form.Ingredients.Count < RecipeFormViewModel.MaxIngredients)
        {
            form.Ingredients.Add(new RecipeIngredientInputViewModel());
        }

        form.ProductOptions = (await productService.GetActiveAsync(cancellationToken))
            .Select(product => new SelectListItem($"{product.Name} ({product.BaseUnit})", product.Id.ToString()))
            .ToList();

        return form;
    }
}
