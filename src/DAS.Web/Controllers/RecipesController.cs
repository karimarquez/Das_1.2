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

    [HttpGet("{id:int}/editar")]
    [Authorize(Roles = AppRoles.Operadores)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var recipe = await recipeService.GetByIdAsync(id, cancellationToken);
        if (recipe is null)
        {
            return NotFound();
        }

        return View("Create", await PrepareFormAsync(recipe.ToFormViewModel(), cancellationToken));
    }

    [HttpPost("{id:int}/editar")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, RecipeFormViewModel input, CancellationToken cancellationToken)
    {
        if (await recipeService.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotFound();
        }

        input.Id = id;
        if (ModelState.IsValid)
        {
            var result = await recipeService.UpdateAsync(id, input.ToDto(), cancellationToken);
            if (result.Succeeded)
            {
                TempData.Success($"La receta {result.Value!.Name} se actualizó.");
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddServiceError(result);
        }

        return View("Create", await PrepareFormAsync(input, cancellationToken));
    }

    [HttpPost("{id:int}/eliminar")]
    [Authorize(Roles = AppRoles.Operadores)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (await recipeService.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotFound();
        }

        var result = await recipeService.DeleteAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData.Error(result.Error!);
        }
        else
        {
            TempData.Success("La receta se eliminó.");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<RecipeFormViewModel> PrepareFormAsync(RecipeFormViewModel form, CancellationToken cancellationToken)
    {
        while (form.Ingredients.Count < RecipeFormViewModel.MaxIngredients)
        {
            form.Ingredients.Add(new RecipeIngredientInputViewModel());
        }

        var selectedIds = form.Ingredients
            .Where(ingredient => ingredient.ProductId.HasValue)
            .Select(ingredient => ingredient.ProductId!.Value)
            .ToHashSet();
        form.ProductOptions = (await productService.GetAllAsync(cancellationToken))
            .Where(product => product.IsActive || selectedIds.Contains(product.Id))
            .Select(product => new SelectListItem(
                $"{product.Name} ({product.BaseUnit}){(product.IsActive ? string.Empty : " (inactivo)")}",
                product.Id.ToString()))
            .ToList();

        return form;
    }
}
