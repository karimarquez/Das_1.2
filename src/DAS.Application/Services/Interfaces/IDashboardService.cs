using DAS.Application.DTOs;

namespace DAS.Application.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(int recentMovements, CancellationToken cancellationToken = default);
}
