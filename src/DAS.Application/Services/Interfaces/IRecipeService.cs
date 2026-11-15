using DAS.Application.Common;
using DAS.Application.DTOs;

namespace DAS.Application.Services.Interfaces;

public interface IRecipeService
{
    Task<IReadOnlyList<RecipeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RecipeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<RecipePreparationDto>> PrepareAsync(PrepareRecipeDto input, CancellationToken cancellationToken = default);
    Task<ServiceResult<RecipeDto>> CreateAsync(SaveRecipeDto input, CancellationToken cancellationToken = default);
    Task<ServiceResult<RecipeDto>> UpdateAsync(int id, SaveRecipeDto input, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
