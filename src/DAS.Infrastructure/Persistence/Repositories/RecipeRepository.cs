using DAS.Application.Abstractions.Persistence;
using DAS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAS.Infrastructure.Persistence.Repositories;

public sealed class RecipeRepository(AppDbContext context) : Repository<Recipe>(context), IRecipeRepository
{
    public async Task<IReadOnlyList<Recipe>> ListWithIngredientsAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(recipe => recipe.Ingredients)
            .OrderBy(recipe => recipe.Id)
            .ToListAsync(cancellationToken);

    public Task<Recipe?> GetWithIngredientsAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(recipe => recipe.Ingredients)
            .FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    public Task<Recipe?> GetTrackedWithIngredientsAsync(int id, CancellationToken cancellationToken = default) =>
        Set.Include(recipe => recipe.Ingredients)
            .FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return Set.AnyAsync(recipe => recipe.Id != excludeId && recipe.Name.ToLower() == normalized, cancellationToken);
    }
}
