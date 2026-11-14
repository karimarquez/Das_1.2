using DAS.Application.DTOs;

namespace DAS.Application.Abstractions;

//Cada formato de exportación es una implementación nueva; no se modifica el servicio ni el controlador
public interface IReportExporter
{
    string Format { get; }
    string ContentType { get; }
    string FileExtension { get; }

    byte[] Export(InventoryReportDto report);
}
