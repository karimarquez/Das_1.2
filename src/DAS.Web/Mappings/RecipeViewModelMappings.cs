using DAS.Application.DTOs;
using DAS.Web.Models.ViewModels;

namespace DAS.Web.Mappings;

public static class RecipeViewModelMappings
{
    public static PrepareRecipeDto ToDto(this RecipeConsumptionInputViewModel input) => new(input.RecipeId, input.Servings);

    public static RecipeFormViewModel ToFormViewModel(this RecipeDto recipe) =>
        new()
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            Ingredients = recipe.Ingredients
                .Select(ingredient => new RecipeIngredientInputViewModel
                {
                    ProductId = ingredient.ProductId,
                    QuantityPerServing = ingredient.QuantityPerServing
                })
                .ToList()
        };

    public static SaveRecipeDto ToDto(this RecipeFormViewModel form) =>
        new(form.Name.Trim(), form.Description?.Trim() ?? string.Empty, form.Ingredients
            .Where(ingredient => ingredient.ProductId.HasValue && ingredient.QuantityPerServing.HasValue)
            .Select(ingredient => new RecipeIngredientInputDto(ingredient.ProductId!.Value, ingredient.QuantityPerServing!.Value))
            .ToList());
}
