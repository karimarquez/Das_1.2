using DAS.Application.Abstractions.Persistence;
using DAS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAS.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(AppDbContext context) : Repository<InventoryProduct>(context), IProductRepository
{
    public async Task<IReadOnlyList<InventoryProduct>> ListOrderedAsync(bool activeOnly, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(product => !activeOnly || product.IsActive)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryProduct>> SearchActiveAsync(string? term, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Where(product => product.IsActive);
        if (!string.IsNullOrWhiteSpace(term))
        {
            var text = term.Trim().ToLower();
            query = query.Where(product => product.Name.ToLower().Contains(text) || product.Category.ToLower().Contains(text));
        }

        return await query
            .OrderBy(product => product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryProduct>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) =>
        await Set.Where(product => ids.Contains(product.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryProduct>> ListForReportAsync(IReadOnlyCollection<int> includedIds, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(product => product.IsActive || includedIds.Contains(product.Id))
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return Set.AnyAsync(product => product.Id != excludeId && product.Name.ToLower() == normalized, cancellationToken);
    }
}
