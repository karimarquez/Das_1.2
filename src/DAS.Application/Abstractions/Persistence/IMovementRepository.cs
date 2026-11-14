using DAS.Application.DTOs;
using DAS.Domain.Entities;

namespace DAS.Application.Abstractions.Persistence;

public interface IMovementRepository : IRepository<InventoryMovement>
{
    //Más recientes primero; From y To se interpretan como días completos
    Task<IReadOnlyList<InventoryMovement>> SearchAsync(MovementQueryDto query, CancellationToken cancellationToken = default);
}
