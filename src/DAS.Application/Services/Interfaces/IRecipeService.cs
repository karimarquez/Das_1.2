using DAS.Application.Common;
using DAS.Application.DTOs;

namespace DAS.Application.Services.Interfaces;

public interface IRecipeService
{
    Task<IReadOnlyList<RecipeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<RecipePreparationDto>> PrepareAsync(PrepareRecipeDto input, CancellationToken cancellationToken = default);
    Task<ServiceResult<RecipeDto>> CreateAsync(SaveRecipeDto input, CancellationToken cancellationToken = default);
}
