using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Drawing; // Do klasy Font
using System.Drawing.Text; // WAŻNE: Do PrivateFontCollection
using System.IO;

namespace FastReportService.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
        // Statyczna kolekcja czcionek - ładujemy ją tylko RAZ dla całej aplikacji
        private static readonly PrivateFontCollection _fontCollection = new PrivateFontCollection();
        private static FontFamily _customFontFamily;

        // Statyczny konstruktor - uruchamia się raz przy starcie
        static ReportsController()
        {
            try 
            {
                // Ścieżka do pliku czcionki w kontenerze Docker
              string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "OpenSans-Regular.ttf");
                
                if (System.IO.File.Exists(fontPath))
                {
                    // Ładujemy czcionkę standardowym mechanizmem .NET (omijamy FastReport Config)
                    _fontCollection.AddFontFile(fontPath);
                    
                    if (_fontCollection.Families.Length > 0)
                    {
                        _customFontFamily = _fontCollection.Families[0];
                        Console.WriteLine($"[INFO] Pomyślnie załadowano czcionkę: {_customFontFamily.Name}");
                    }
                }
                else
                {
                    Console.WriteLine($"[ERROR] Brak pliku czcionki w: {fontPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Krytyczny błąd ładowania czcionki: {ex.Message}");
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
            table.Rows.Add("Latwe", "15");
            table.Rows.Add("Trudne", "8");
            table.Rows.Add("Srednia", "4.5");

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
            
            // Tutaj następuje "magia" naprawy czcionek
            FixFonts(report); 
            
            return report;
        }

        // Nowa metoda FixFonts korzystająca z PrivateFontCollection
        private void FixFonts(Report report)
        {
            // Jeśli nie udało się załadować czcionki, nic nie robimy (unikamy crasha)
            if (_customFontFamily == null) return;

            foreach (Base obj in report.AllObjects)
            {
                if (obj is FastReport.TextObject textObj)
                {
                    // Tworzymy nową czcionkę korzystając z fizycznie załadowanej rodziny (Open Sans)
                    // Zachowujemy oryginalny rozmiar (Size) i styl (Style) z szablonu
                    textObj.Font = new Font(_customFontFamily, textObj.Font.Size, textObj.Font.Style);
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
            
            var dataBand = report.FindObject("ListBand") as FastReport.DataBand;
            if (dataBand != null) dataBand.DataSource = report.GetDataSource("Dane");
            
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
