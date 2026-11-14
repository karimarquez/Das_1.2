using DAS.Application.DTOs;

namespace DAS.Application.Services.Interfaces;

//Consultas separadas de los comandos: quien solo lee el catálogo no depende de las operaciones de escritura
public interface IProductQueryService
{
    Task<IReadOnlyList<ProductDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductStockDto>> SearchStockAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
}
