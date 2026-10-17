using DAS.Models.DTOs;

namespace DAS.Models.ViewModels;

public class RecipesViewModel
{
    public IReadOnlyList<RecipeDto> Recipes { get; init; } = [];
}
