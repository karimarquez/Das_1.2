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
        var movements = Filter(query);
        movements = OrderRecentFirst(movements);
        if (query.Take is int take)
        {
            movements = movements.Take(Math.Clamp(take, 1, 1000));
        }

        return await movements.ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<InventoryMovement> Items, int Page, int TotalCount)> SearchPageAsync(
        MovementQueryDto query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var filtered = Filter(query);
        var totalCount = await filtered.CountAsync(cancellationToken);
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, pageCount);

        var items = await OrderRecentFirst(filtered)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, page, totalCount);
    }

    public async Task<IReadOnlyList<MovementReportValueDto>> GetReportValuesAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var start = from.Date;
        var end = to.Date.AddDays(1);
        return await Set.AsNoTracking()
            .Where(movement => movement.RecordedAt >= start && movement.RecordedAt < end)
            .Select(movement => new MovementReportValueDto(
                movement.ProductId,
                movement.Type,
                movement.Quantity,
                movement.StockBefore,
                movement.StockAfter))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovementTypeCountDto>> CountByProductAndTypeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var start = from.Date;
        var end = to.Date.AddDays(1);
        var grouped =
            from movement in Set.AsNoTracking()
            join product in Context.Products.AsNoTracking() on movement.ProductId equals product.Id
            where movement.RecordedAt >= start && movement.RecordedAt < end
            group movement by new { product.Id, product.Name, movement.Type } into productMovements
            orderby productMovements.Key.Name, productMovements.Key.Type
            select new MovementTypeCountDto(
                productMovements.Key.Id,
                productMovements.Key.Name,
                productMovements.Key.Type,
                productMovements.Count());

        return await grouped.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovementCountDto>> CountByTypeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var start = from.Date;
        var end = to.Date.AddDays(1);
        return await Set.AsNoTracking()
            .Where(movement => movement.RecordedAt >= start && movement.RecordedAt < end)
            .GroupBy(movement => movement.Type)
            .OrderBy(group => group.Key)
            .Select(group => new MovementCountDto(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<InventoryMovement> Filter(MovementQueryDto query)
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

        return movements;
    }

    private static IOrderedQueryable<InventoryMovement> OrderRecentFirst(IQueryable<InventoryMovement> movements) =>
        movements.OrderByDescending(movement => movement.RecordedAt).ThenByDescending(movement => movement.Id);
}
