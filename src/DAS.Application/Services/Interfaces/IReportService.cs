using DAS.Application.DTOs;
using DAS.Domain.Enums;

namespace DAS.Application.Services.Interfaces;

//Solo arma el reporte; el formato de archivo lo resuelve un IReportExporter
public interface IReportService
{
    Task<InventoryReportDto> BuildAsync(ReportPeriod period, DateTime referenceDate, CancellationToken cancellationToken = default);
}
