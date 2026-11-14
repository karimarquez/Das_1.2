using DAS.Domain.Entities;

namespace DAS.Application.Abstractions.Persistence;

public interface IRecipeRepository : IRepository<Recipe>
{
    Task<IReadOnlyList<Recipe>> ListWithIngredientsAsync(CancellationToken cancellationToken = default);

    Task<Recipe?> GetWithIngredientsAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
