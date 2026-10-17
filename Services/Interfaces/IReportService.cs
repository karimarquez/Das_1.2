using DAS.Models.DTOs;
using DAS.Models.Enums;

namespace DAS.Services.Interfaces;

public interface IReportService
{
    InventoryReportDto Build(ReportPeriod period, DateTime referenceDate);
    byte[] ExportCsv(InventoryReportDto report);
}
