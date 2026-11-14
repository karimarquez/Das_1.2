using DAS.Application.DTOs;

namespace DAS.Web.Models.ViewModels;

public class RecipesViewModel
{
    public IReadOnlyList<RecipeDto> Recipes { get; init; } = [];
}
