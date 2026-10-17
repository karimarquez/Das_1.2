using DAS.Models.DTOs;

namespace DAS.Services.Interfaces;

public interface IPhysicalCountService
{
    ServiceResult<PhysicalCountDto> Register(IReadOnlyCollection<CountLineDto> lines);
    PhysicalCountDto? GetById(int id);
    IReadOnlyList<PhysicalCountDto> GetRecent(int take);
}
