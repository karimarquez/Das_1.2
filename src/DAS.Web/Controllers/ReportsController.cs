using DAS.Application.Abstractions;
using DAS.Application.Services.Interfaces;
using DAS.Domain.Enums;
using DAS.Web.Mappings;
using DAS.Web.Models.Validation;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Web.Controllers;

[Route("reportes")]
public class ReportsController(IReportService reportService, IEnumerable<IReportExporter> exporters) : Controller
{
    //El segmento {period} solo acepta "diario" o "mensual"; cualquier otro valor responde 404
    [HttpGet("{period:regex(^(diario|mensual)$)=mensual}")]
    public async Task<IActionResult> Index(ReportPeriod period, [FromQuery(Name = "fecha")][NotFutureDate] DateTime? date, CancellationToken cancellationToken)
    {
        var referenceDate = DateTime.Today;
        if (!ModelState.IsValid)
        {
            ViewData["DateError"] = "La fecha del reporte no puede ser futura; se muestra el período actual.";
        }
        else if (date.HasValue)
        {
            referenceDate = date.Value.Date;
        }

        var report = await reportService.BuildAsync(period, referenceDate, cancellationToken);
        return View(report.ToViewModel(referenceDate));
    }

    //El formato elige el exportador registrado; agregar otro formato no requiere cambiar esta acción
    [HttpGet("{period:regex(^(diario|mensual)$)}/exportar")]
    public async Task<IActionResult> Export(
        ReportPeriod period,
        [FromQuery(Name = "fecha")] DateTime? date,
        [FromQuery(Name = "formato")] string? format,
        CancellationToken cancellationToken)
    {
        var exporter = exporters.FirstOrDefault(item => string.Equals(item.Format, format ?? "csv", StringComparison.OrdinalIgnoreCase));
        if (exporter is null)
        {
            return NotFound();
        }

        var referenceDate = date.HasValue && date.Value.Date <= DateTime.Today ? date.Value.Date : DateTime.Today;
        var report = await reportService.BuildAsync(period, referenceDate, cancellationToken);
        var fileName = $"reporte-{period.ToString().ToLowerInvariant()}-{report.From:yyyy-MM-dd}.{exporter.FileExtension}";

        return File(exporter.Export(report), exporter.ContentType, fileName);
    }
}
