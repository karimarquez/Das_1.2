using DAS.Application.Common;
using DAS.Application.DTOs;

namespace DAS.Application.Services.Interfaces;

public interface IPhysicalCountService
{
    Task<ServiceResult<PhysicalCountDto>> RegisterAsync(IReadOnlyCollection<CountLineDto> lines, CancellationToken cancellationToken = default);
    Task<PhysicalCountDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalCountDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
