using DAS.Domain.Entities;

namespace DAS.Application.Abstractions.Persistence;

public interface IPhysicalCountRepository : IRepository<PhysicalCount>
{
    Task<PhysicalCount?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhysicalCount>> ListRecentAsync(int take, CancellationToken cancellationToken = default);
}
