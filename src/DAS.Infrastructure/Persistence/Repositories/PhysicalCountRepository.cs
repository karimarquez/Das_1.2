using DAS.Application.Abstractions.Persistence;
using DAS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAS.Infrastructure.Persistence.Repositories;

public sealed class PhysicalCountRepository(AppDbContext context) : Repository<PhysicalCount>(context), IPhysicalCountRepository
{
    public Task<PhysicalCount?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(count => count.Lines)
            .FirstOrDefaultAsync(count => count.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PhysicalCount>> ListRecentAsync(int take, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(count => count.Lines)
            .OrderByDescending(count => count.CountedAt)
            .ThenByDescending(count => count.Id)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);
}
