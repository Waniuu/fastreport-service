using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace FastReportService.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
        // 1. Raport testowy (już go masz)
        [HttpGet("test")]
        public IActionResult GetTest()
        {
            return GenerateReport("Reports/raport_testow.frx");
        }

        // 2. Raport: Statystyki pytań (Wykres)
        // Oczekuje: ?id_banku=1&id_kategorii=2
        [HttpGet("questions-stats")]
        public IActionResult GetQuestionsStats([FromQuery] int? id_banku, [FromQuery] int? id_kategorii)
        {
            // Docelowo zmień na "Reports/questions_stats.frx"
            var report = new Report();
            report.Load("Reports/raport_testow.frx"); 

            // Przekazywanie parametrów do raportu (jeśli raport ich używa)
            report.SetParameterValue("id_banku", id_banku ?? 0);
            report.SetParameterValue("id_kategorii", id_kategorii ?? 0);

            return ExportToPdf(report, "statystyki_pytan.pdf");
        }

        // 3. Raport: Lista użytkowników
        // Oczekuje: ?rola=student&email=...
        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] string? rola, [FromQuery] string? email)
        {
            // Docelowo zmień na "Reports/users_list.frx"
            var report = new Report();
            report.Load("Reports/raport_testow.frx");

            report.SetParameterValue("rola", rola ?? "");
            report.SetParameterValue("email", email ?? "");

            return ExportToPdf(report, "lista_uzytkownikow.pdf");
        }

        // 4. Raport: Testy pogrupowane
        // Oczekuje: ?start=2024-01-01&end=2024-12-31
        [HttpGet("tests-grouped")]
        public IActionResult GetTestsGrouped([FromQuery] string? start, [FromQuery] string? end)
        {
            // Docelowo zmień na "Reports/tests_grouped.frx"
            var report = new Report();
            report.Load("Reports/raport_testow.frx");

            report.SetParameterValue("DataOd", start ?? "");
            report.SetParameterValue("DataDo", end ?? "");

            return ExportToPdf(report, "testy_grupowane.pdf");
        }

        // 5. Raport: Formularz / Karta egzaminacyjna
        // Oczekuje: ?id_testu=5&id_uzytkownika=10
        [HttpGet("test-form")]
        public IActionResult GetTestForm([FromQuery] int? id_testu, [FromQuery] int? id_uzytkownika)
        {
            // Docelowo zmień na "Reports/exam_card.frx"
            var report = new Report();
            report.Load("Reports/raport_testow.frx");

            report.SetParameterValue("id_testu", id_testu ?? 0);
            report.SetParameterValue("id_uzytkownika", id_uzytkownika ?? 0);

            return ExportToPdf(report, "karta_egzaminacyjna.pdf");
        }

        // --- Metoda pomocnicza do generowania PDF ---
        private IActionResult GenerateReport(string reportPath)
        {
            Report report = new Report();
            report.Load(reportPath);
            return ExportToPdf(report, "raport.pdf");
        }

        private IActionResult ExportToPdf(Report report, string fileName)
        {
            report.Prepare();

            using var ms = new MemoryStream();
            var pdf = new PDFSimpleExport();
            report.Export(pdf, ms);
            ms.Position = 0;

            return File(ms.ToArray(), "application/pdf", fileName);
        }
    }
}
