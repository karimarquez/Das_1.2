using DAS.Models.DTOs;

namespace DAS.Services.Interfaces;

public interface IRecipeService
{
    IReadOnlyList<RecipeDto> GetAll();
    ServiceResult<RecipePreparationDto> Prepare(PrepareRecipeDto preparation);
    ServiceResult<RecipeDto> Create(SaveRecipeDto recipe);
}
