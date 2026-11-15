using DAS.Application.Abstractions.Persistence;
using DAS.Application.DTOs;
using DAS.Domain.Common;
using DAS.Domain.Entities;

namespace DAS.Tests.Fakes;

//Sustituye a los repositorios de EF Core en las pruebas unitarias: mismos contratos, datos en listas
public class InMemoryRepository<T> : IRepository<T> where T : Entity
{
    private readonly List<T> _pending = [];
    private int _lastId;

    public List<T> Items { get; } = [];

    public void Seed(params T[] entities)
    {
        foreach (var entity in entities)
        {
            if (entity.Id == 0)
            {
                entity.Id = ++_lastId;
            }

            _lastId = Math.Max(_lastId, entity.Id);
            Items.Add(entity);
        }
    }

    public int NextId() => ++_lastId;

    public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(entity => entity.Id == id));

    public Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<T>>(Items.ToList());

    public void Add(T entity) => _pending.Add(entity);

    public void Update(T entity)
    {
    }

    public void Remove(T entity) => Items.Remove(entity);

    //Como SaveChanges: lo agregado recibe Id y pasa a ser consultable
    public int Commit()
    {
        var saved = _pending.Count;
        foreach (var entity in _pending)
        {
            entity.Id = ++_lastId;
            Items.Add(entity);
        }

        _pending.Clear();
        return saved;
    }

    public void Discard() => _pending.Clear();

    protected static Task<IReadOnlyList<TItem>> Result<TItem>(IEnumerable<TItem> items) =>
        Task.FromResult<IReadOnlyList<TItem>>(items.ToList());
}

public sealed class InMemoryProductRepository : InMemoryRepository<InventoryProduct>, IProductRepository
{
    public (int Page, int PageSize)? LastSearchPaging { get; private set; }

    public Task<IReadOnlyList<InventoryProduct>> ListOrderedAsync(bool activeOnly, CancellationToken cancellationToken = default) =>
        Result(Items.Where(product => !activeOnly || product.IsActive).OrderBy(product => product.Name));

    public Task<IReadOnlyList<InventoryProduct>> SearchActiveAsync(string? term, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        LastSearchPaging = (page, pageSize);
        var found = Items.Where(product => product.IsActive
            && (string.IsNullOrWhiteSpace(term)
                || product.Name.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase)
                || product.Category.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase)));
        return Result(found.OrderBy(product => product.Name).Skip((page - 1) * pageSize).Take(pageSize));
    }

    public Task<IReadOnlyList<InventoryProduct>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) =>
        Result(Items.Where(product => ids.Contains(product.Id)));

    public Task<IReadOnlyList<InventoryProduct>> ListForReportAsync(IReadOnlyCollection<int> includedIds, CancellationToken cancellationToken = default) =>
        Result(Items.Where(product => product.IsActive || includedIds.Contains(product.Id)).OrderBy(product => product.Name));

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(product => product.Id != excludeId && string.Equals(product.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));
}

public sealed class InMemoryMovementRepository : InMemoryRepository<InventoryMovement>, IMovementRepository
{
    public Task<IReadOnlyList<InventoryMovement>> SearchAsync(MovementQueryDto query, CancellationToken cancellationToken = default)
    {
        var found = Filter(query);
        return Result(query.Take is int take ? found.Take(take) : found);
    }

    public Task<(IReadOnlyList<InventoryMovement> Items, int Page, int TotalCount)> SearchPageAsync(
        MovementQueryDto query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var found = Filter(query).ToList();
        var totalCount = found.Count;
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, pageCount);
        IReadOnlyList<InventoryMovement> items = found.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, page, totalCount));
    }

    public Task<IReadOnlyList<MovementReportValueDto>> GetReportValuesAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default) =>
        Result(Items
            .Where(movement => movement.RecordedAt >= from.Date && movement.RecordedAt < to.Date.AddDays(1))
            .Select(movement => new MovementReportValueDto(
                movement.ProductId,
                movement.Type,
                movement.Quantity,
                movement.StockBefore,
                movement.StockAfter)));

    public Task<IReadOnlyList<MovementTypeCountDto>> CountByProductAndTypeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default) =>
        Result(Items
            .Where(movement => movement.RecordedAt >= from.Date && movement.RecordedAt < to.Date.AddDays(1))
            .GroupBy(movement => new { movement.ProductId, movement.ProductName, movement.Type })
            .OrderBy(group => group.Key.ProductName)
            .ThenBy(group => group.Key.Type)
            .Select(group => new MovementTypeCountDto(
                group.Key.ProductId,
                group.Key.ProductName,
                group.Key.Type,
                group.Count())));

    public Task<IReadOnlyList<MovementCountDto>> CountByTypeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default) =>
        Result(Items
            .Where(movement => movement.RecordedAt >= from.Date && movement.RecordedAt < to.Date.AddDays(1))
            .GroupBy(movement => movement.Type)
            .OrderBy(group => group.Key)
            .Select(group => new MovementCountDto(group.Key, group.Count())));

    private IEnumerable<InventoryMovement> Filter(MovementQueryDto query) =>
        Items
            .Where(movement => query.ProductId is null || movement.ProductId == query.ProductId)
            .Where(movement => query.Type is null || movement.Type == query.Type)
            .Where(movement => query.From is null || movement.RecordedAt >= query.From.Value.Date)
            .Where(movement => query.To is null || movement.RecordedAt < query.To.Value.Date.AddDays(1))
            .OrderByDescending(movement => movement.RecordedAt)
            .ThenByDescending(movement => movement.Id);
}

public sealed class InMemoryRecipeRepository : InMemoryRepository<Recipe>, IRecipeRepository
{
    public Task<IReadOnlyList<Recipe>> ListWithIngredientsAsync(CancellationToken cancellationToken = default) =>
        Result(Items.OrderBy(recipe => recipe.Id));

    public Task<Recipe?> GetWithIngredientsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(recipe => recipe.Id == id));

    public Task<Recipe?> GetTrackedWithIngredientsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(recipe => recipe.Id == id));

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(recipe => recipe.Id != excludeId && string.Equals(recipe.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));
}

public sealed class InMemoryPhysicalCountRepository : InMemoryRepository<PhysicalCount>, IPhysicalCountRepository
{
    public Task<PhysicalCount?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(count => count.Id == id));

    public Task<IReadOnlyList<PhysicalCount>> ListRecentAsync(int take, CancellationToken cancellationToken = default) =>
        Result(Items.OrderByDescending(count => count.CountedAt).ThenByDescending(count => count.Id).Take(take));
}
