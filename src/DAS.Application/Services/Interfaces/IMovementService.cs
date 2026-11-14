using DAS.Application.Common;
using DAS.Application.DTOs;

namespace DAS.Application.Services.Interfaces;

public interface IMovementService
{
    Task<IReadOnlyList<MovementDto>> SearchAsync(MovementQueryDto query, CancellationToken cancellationToken = default);
    Task<ServiceResult<MovementDto>> RegisterAsync(RegisterMovementDto input, CancellationToken cancellationToken = default);
}
