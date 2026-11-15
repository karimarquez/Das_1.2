using DAS.Application.Common;
using DAS.Application.DTOs;

namespace DAS.Application.Services.Interfaces;

public interface IMovementService
{
    Task<IReadOnlyList<MovementDto>> SearchAsync(MovementQueryDto query, CancellationToken cancellationToken = default);
    Task<MovementPageDto> SearchPageAsync(MovementQueryDto query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovementCountDto>> CountByTypeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<ServiceResult<MovementDto>> RegisterAsync(RegisterMovementDto input, CancellationToken cancellationToken = default);
}
