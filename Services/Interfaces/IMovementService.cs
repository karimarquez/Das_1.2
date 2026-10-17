using DAS.Models.DTOs;

namespace DAS.Services.Interfaces;

public interface IMovementService
{
    IReadOnlyList<MovementDto> Search(MovementQueryDto query);
    ServiceResult<MovementDto> Register(RegisterMovementDto movement);
}
