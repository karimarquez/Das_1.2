using DAS.Application.DTOs;
using DAS.Domain.Entities;

namespace DAS.Application.Mappings;

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
}
