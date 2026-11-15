using DAS.Application.DTOs;
using DAS.Domain.Entities;

namespace DAS.Application.Abstractions.Persistence;

public interface IMovementRepository : IRepository<InventoryMovement>
{
    //Más recientes primero; From y To se interpretan como días completos
    Task<IReadOnlyList<InventoryMovement>> SearchAsync(MovementQueryDto query, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryMovement> Items, int Page, int TotalCount)> SearchPageAsync(
        MovementQueryDto query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovementReportValueDto>> GetReportValuesAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovementTypeCountDto>> CountByProductAndTypeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovementCountDto>> CountByTypeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
