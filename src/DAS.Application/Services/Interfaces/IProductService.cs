using DAS.Application.Common;
using DAS.Application.DTOs;

namespace DAS.Application.Services.Interfaces;

public interface IProductService : IProductQueryService
{
    Task<ServiceResult<ProductDto>> CreateAsync(SaveProductDto input, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductDto>> UpdateAsync(int id, SaveProductDto input, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductDto>> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
