using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Drawing; // Wymagane dla klasy Font
using System.IO;

namespace FastReportService.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
        // Statyczny konstruktor - ładuje czcionkę RAZ przy starcie aplikacji
        static ReportsController()
        {
            try 
            {
                // Ścieżka do pliku czcionki
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "OpenSans-Regular.ttf");
                
                if (System.IO.File.Exists(fontPath))
                {
                    // Rejestrujemy czcionkę w FastReport
                    Config.FontSettings.AddFont(fontPath);
                    Console.WriteLine($"[INFO] Załadowano czcionkę: {fontPath}");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Nie znaleziono pliku czcionki: {fontPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Błąd ładowania czcionki: {ex.Message}");
            }
        }

        // ==========================================
        // 1. RAPORT: Statystyki pytań
        // ==========================================
        [HttpGet("questions-stats")]
        public IActionResult GetQuestionsStats([FromQuery] int? id_banku, [FromQuery] int? id_kategorii)
        {
            var report = LoadAndFixReport();

            SetText(report, "Title", "Raport: Statystyki Pytań");
            SetText(report, "Panel1Header", "Wybrane kryteria");
            SetText(report, "Panel1Body", $"Bank ID: {id_banku ?? 0}, Kategoria ID: {id_kategorii ?? 0}");
            SetText(report, "Panel2Header", "Podsumowanie");
            SetText(report, "Panel2Body", "Poniżej znajduje się zestawienie trudności pytań.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Łatwe", "15"); // Używam polskich znaków testowo
            table.Rows.Add("Trudne", "8");
            table.Rows.Add("Średnia", "4.5");

            return ExportReport(report, table, "statystyki.pdf");
        }

        // ==========================================
        // 2. RAPORT: Lista użytkowników
        // ==========================================
        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] string? rola, [FromQuery] string? email)
        {
            var report = LoadAndFixReport();

            SetText(report, "Title", "Lista Użytkowników");
            SetText(report, "Panel1Header", "Filtrowanie");
            SetText(report, "Panel1Body", $"Rola: {rola ?? "Wszystkie"}, Email: {email ?? "-"}");
            SetText(report, "Panel2Header", "Informacja");
            SetText(report, "Panel2Body", "Wygenerowano listę użytkowników.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Jan Kowalski", "Student");
            table.Rows.Add("Anna Nowak", "Nauczyciel");

            return ExportReport(report, table, "uzytkownicy.pdf");
        }

        // ==========================================
        // 3. RAPORT: Testy pogrupowane
        // ==========================================
        [HttpGet("tests-grouped")]
        public IActionResult GetTestsGrouped([FromQuery] string? start, [FromQuery] string? end)
        {
            var report = LoadAndFixReport();
            SetText(report, "Title", "Raport Testów");
            SetText(report, "Panel1Header", "Zakres dat");
            SetText(report, "Panel1Body", $"Od: {start} Do: {end}");
            SetText(report, "Panel2Header", "Status");
            SetText(report, "Panel2Body", "Wydajność systemu.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Egzamin SQL", "2025-05-10");

            return ExportReport(report, table, "testy.pdf");
        }

        // ==========================================
        // 4. RAPORT: Karta egzaminacyjna
        // ==========================================
        [HttpGet("test-form")]
        public IActionResult GetTestForm([FromQuery] int? id_testu, [FromQuery] int? id_uzytkownika)
        {
            var report = LoadAndFixReport();
            SetText(report, "Title", "KARTA EGZAMINACYJNA");
            SetText(report, "Panel1Header", "Dane Studenta");
            SetText(report, "Panel1Body", $"Student ID: {id_uzytkownika}");
            SetText(report, "Panel2Header", "Wynik");
            SetText(report, "Panel2Body", "Zaliczono.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Pytanie 1", "OK");

            return ExportReport(report, table, "karta.pdf");
        }

        // ==========================================
        // METODY POMOCNICZE
        // ==========================================

        private Report LoadAndFixReport()
        {
            var report = new Report();
            report.Load("Reports/raport_testow.frx");
            FixFonts(report); // Podmieniamy fonty na załadowany plik
            return report;
        }

        private void FixFonts(Report report)
        {
            // Nazwa czcionki musi się zgadzać z nazwą pliku TTF (Family Name)
            // Dla OpenSans-Regular.ttf to zazwyczaj "Open Sans"
            string safeFontName = "Open Sans"; 

            foreach (Base obj in report.AllObjects)
            {
                if (obj is FastReport.TextObject textObj)
                {
                    // Wymuszamy użycie naszej czcionki
                    textObj.Font = new Font(safeFontName, textObj.Font.Size, textObj.Font.Style);
                }
            }
        }

        private void SetText(Report report, string objectName, string text)
        {
            var obj = report.FindObject(objectName) as FastReport.TextObject;
            if (obj != null) obj.Text = text;
        }

        private IActionResult ExportReport(Report report, DataTable data, string fileName)
        {
            report.RegisterData(data, "Dane");
            
            // Podpięcie danych do DataBand
            var dataBand = report.FindObject("ListBand") as FastReport.DataBand;
            if (dataBand != null) dataBand.DataSource = report.GetDataSource("Dane");
            
            // Podpięcie danych do pól tekstowych w liście
            var listItem = report.FindObject("ListItem") as FastReport.TextObject;
            if(listItem != null) listItem.Text = "[Dane.Nazwa]: [Dane.Wartosc]";

            report.Prepare();

            using var ms = new MemoryStream();
            var pdf = new PDFSimpleExport();
            report.Export(pdf, ms);
            ms.Position = 0;

            return File(ms.ToArray(), "application/pdf", fileName);
        }
    }
}
