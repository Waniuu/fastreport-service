using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Drawing; // Ważne dla Font
using System.IO;

namespace FastReportService.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
        // ==========================================
        // 1. RAPORT: Statystyki pytań
        // ==========================================
        [HttpGet("questions-stats")]
        public IActionResult GetQuestionsStats([FromQuery] int? id_banku, [FromQuery] int? id_kategorii)
        {
            var report = LoadAndFixReport(); // <-- Używamy nowej metody ładującej i naprawiającej

            // Podmieniamy teksty (bez zmiany fontu tutaj, bo zrobi to FixFonts)
            SetText(report, "Title", "Raport: Statystyki Pytań");
            SetText(report, "Panel1Header", "Wybrane kryteria");
            SetText(report, "Panel1Body", $"Bank ID: {id_banku ?? 0}, Kategoria ID: {id_kategorii ?? 0}");
            SetText(report, "Panel2Header", "Podsumowanie");
            SetText(report, "Panel2Body", "Poniżej znajduje się zestawienie trudności pytań.");

            // Symulacja danych
            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Liczba pytań łatwych", "15");
            table.Rows.Add("Liczba pytań trudnych", "8");
            table.Rows.Add("Średnia punktów", "4.5");

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
            table.Rows.Add("Piotr Wiśniewski", "Administrator");

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
            SetText(report, "Panel2Body", "Wydajność systemu testowego.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Egzamin SQL", "2025-05-10");
            table.Rows.Add("Kolokwium C#", "2025-06-01");

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
            SetText(report, "Panel1Body", $"ID Studenta: {id_uzytkownika}, ID Testu: {id_testu}");
            SetText(report, "Panel2Header", "Wynik");
            SetText(report, "Panel2Body", "Egzamin zaliczony pozytywnie.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Pytanie 1", "Poprawna");
            table.Rows.Add("Pytanie 2", "Błędna");

            return ExportReport(report, table, "karta.pdf");
        }

        // ==========================================
        // METODY POMOCNICZE
        // ==========================================

        private Report LoadAndFixReport()
        {
            var report = new Report();
            // Upewnij się, że plik .frx jest w folderze Reports
            report.Load("Reports/raport_testow.frx");
            
            // KLUCZOWE: Podmiana czcionek na działające na Linuxie
            FixFonts(report);
            
            return report;
        }

        // Metoda "Atomowa" - naprawia czcionki w całym raporcie
        private void FixFonts(Report report)
        {
            // Przechodzimy przez wszystkie obiekty w raporcie
            foreach (Base obj in report.AllObjects)
            {
                if (obj is FastReport.TextObject textObj)
                {
                    // Ustawiamy bezpieczną czcionkę systemową
                    // Jeśli Arial nie zadziała, spróbuj "DejaVu Sans" lub "Liberation Sans"
                    try 
                    {
                        textObj.Font = new Font("Arial", textObj.Font.Size, textObj.Font.Style);
                    }
                    catch
                    {
                        // Fallback, jeśli Arial nie istnieje
                        textObj.Font = new Font(FontFamily.GenericSansSerif, textObj.Font.Size, textObj.Font.Style);
                    }
                }
            }
        }

        private void SetText(Report report, string objectName, string text)
        {
            var obj = report.FindObject(objectName) as FastReport.TextObject;
            if (obj != null)
            {
                obj.Text = text;
                // Tutaj już nie musimy zmieniać fontu, robi to FixFonts()
            }
        }

        private IActionResult ExportReport(Report report, DataTable data, string fileName)
        {
            report.RegisterData(data, "Dane");

            var dataBand = report.FindObject("ListBand") as FastReport.DataBand;
            if (dataBand != null)
            {
                dataBand.DataSource = report.GetDataSource("Dane");
            }
            
            var listItem = report.FindObject("ListItem") as FastReport.TextObject;
            if(listItem != null)
            {
                listItem.Text = "[Dane.Nazwa]: [Dane.Wartosc]";
            }

            report.Prepare();

            using var ms = new MemoryStream();
            var pdf = new PDFSimpleExport();
            report.Export(pdf, ms);
            ms.Position = 0;

            return File(ms.ToArray(), "application/pdf", fileName);
        }
    }
}
