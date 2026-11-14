using DAS.Application.Abstractions.Persistence;
using DAS.Application.DTOs;
using DAS.Domain.Entities;
using DAS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DAS.Infrastructure.Persistence.Repositories;

public sealed class MovementRepository(AppDbContext context) : Repository<InventoryMovement>(context), IMovementRepository
{
    public async Task<IReadOnlyList<InventoryMovement>> SearchAsync(MovementQueryDto query, CancellationToken cancellationToken = default)
    {
        var movements = Set.AsNoTracking();
        if (query.ProductId is int productId)
        {
            movements = movements.Where(movement => movement.ProductId == productId);
        }

        if (query.Type is MovementType type)
        {
            movements = movements.Where(movement => movement.Type == type);
        }

        if (query.From is DateTime from)
        {
            var start = from.Date;
            movements = movements.Where(movement => movement.RecordedAt >= start);
        }

        if (query.To is DateTime to)
        {
            var end = to.Date.AddDays(1);
            movements = movements.Where(movement => movement.RecordedAt < end);
        }

        movements = movements.OrderByDescending(movement => movement.RecordedAt).ThenByDescending(movement => movement.Id);
        if (query.Take is int take)
        {
            movements = movements.Take(Math.Clamp(take, 1, 1000));
        }

        return await movements.ToListAsync(cancellationToken);
    }
}
