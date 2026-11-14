using DAS.Domain.Entities;

namespace DAS.Application.Abstractions.Persistence;

public interface IProductRepository : IRepository<InventoryProduct>
{
    Task<IReadOnlyList<InventoryProduct>> ListOrderedAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryProduct>> SearchActiveAsync(string? term, int page, int pageSize, CancellationToken cancellationToken = default);

    //Rastreados: se usan para registrar movimientos sobre varios productos a la vez
    Task<IReadOnlyList<InventoryProduct>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    //Activos más los inactivos que tuvieron movimientos en el período
    Task<IReadOnlyList<InventoryProduct>> ListForReportAsync(IReadOnlyCollection<int> includedIds, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
}
