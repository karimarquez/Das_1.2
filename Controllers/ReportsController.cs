using DAS.Mappings;
using DAS.Models.Enums;
using DAS.Models.Validation;
using DAS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Controllers;

[Route("reportes")]
public class ReportsController(IReportService reportService) : Controller
{
    //El segmento {period} solo acepta "diario" o "mensual"; cualquier otro valor responde 404
    [HttpGet("{period:regex(^(diario|mensual)$)=mensual}")]
    public IActionResult Index(ReportPeriod period, [FromQuery(Name = "fecha")][NotFutureDate] DateTime? date)
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

        var report = reportService.Build(period, referenceDate);
        return View(report.ToViewModel(referenceDate));
    }

    [HttpGet("{period:regex(^(diario|mensual)$)}/exportar")]
    public IActionResult Export(ReportPeriod period, [FromQuery(Name = "fecha")] DateTime? date)
    {
        var referenceDate = date.HasValue && date.Value.Date <= DateTime.Today ? date.Value.Date : DateTime.Today;
        var report = reportService.Build(period, referenceDate);
        var fileName = $"reporte-{period.ToString().ToLowerInvariant()}-{report.From:yyyy-MM-dd}.csv";

        return File(reportService.ExportCsv(report), "text/csv", fileName);
    }
}
