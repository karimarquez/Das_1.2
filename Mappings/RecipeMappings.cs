using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.ViewModels;

namespace DAS.Mappings;

public static class RecipeMappings
{
    public static RecipeDto ToDto(this Recipe recipe, IReadOnlyDictionary<int, InventoryProduct> products) =>
        new(recipe.Id, recipe.Name, recipe.Description, recipe.Ingredients
            .Select(ingredient => new RecipeIngredientDto(
                ingredient.ProductId,
                products.TryGetValue(ingredient.ProductId, out var product) ? product.Name : "Producto no disponible",
                ingredient.QuantityPerServing,
                ingredient.BaseUnit))
            .ToList());

    public static PrepareRecipeDto ToDto(this RecipeConsumptionInputViewModel input) => new(input.RecipeId, input.Servings);

    public static SaveRecipeDto ToDto(this RecipeFormViewModel form) =>
        new(form.Name.Trim(), form.Description?.Trim() ?? string.Empty, form.Ingredients
            .Where(ingredient => ingredient.ProductId.HasValue && ingredient.QuantityPerServing.HasValue)
            .Select(ingredient => new RecipeIngredientInputDto(ingredient.ProductId!.Value, ingredient.QuantityPerServing!.Value))
            .ToList());
}
