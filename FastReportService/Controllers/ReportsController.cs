using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("reports")]
public class ReportsController : ControllerBase
{
    [HttpGet("test")]
    public IActionResult GetPdf()
    {
        Report report = new Report();
        report.Load("Reports/raport_testow.frx");

        report.Prepare();

        using var ms = new MemoryStream();
        var pdf = new PDFSimpleExport();
        report.Export(pdf, ms);
        ms.Position = 0;

        return File(ms.ToArray(), "application/pdf", "raport.pdf");
    }
}
