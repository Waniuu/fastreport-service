using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.AspNetCore.Mvc;
using System.Data;
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
            var report = LoadReport();

            // 1. Podmieniamy tytuły w raporcie
            SetText(report, "Title", "Raport: Statystyki Pytań");
            SetText(report, "Panel1Header", "Wybrane kryteria");
            SetText(report, "Panel1Body", $"Bank ID: {id_banku ?? 0}, Kategoria ID: {id_kategorii ?? 0}");
            
            SetText(report, "Panel2Header", "Podsumowanie");
            SetText(report, "Panel2Body", "Poniżej znajduje się zestawienie trudności pytań.");

            // 2. Wstrzykujemy dane (Symulacja danych z bazy)
            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string)); // To trafi do pola w raporcie
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
            var report = LoadReport();

            SetText(report, "Title", "Lista Użytkowników");
            SetText(report, "Panel1Header", "Filtrowanie");
            SetText(report, "Panel1Body", $"Rola: {rola ?? "Wszystkie"}, Email: {email ?? "-"}");

            SetText(report, "Panel2Header", "Informacja");
            SetText(report, "Panel2Body", "Wygenerowano listę użytkowników spełniających kryteria.");

            // Dane symulowane
            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));

            table.Rows.Add("Jan Kowalski", "Student");
            table.Rows.Add("Anna Nowak", "Nauczyciel");
            table.Rows.Add("Piotr Wiśniewski", "Administrator");
            table.Rows.Add("Maria Zielińska", "Student");

            return ExportReport(report, table, "uzytkownicy.pdf");
        }

        // ==========================================
        // 3. RAPORT: Testy pogrupowane
        // ==========================================
        [HttpGet("tests-grouped")]
        public IActionResult GetTestsGrouped([FromQuery] string? start, [FromQuery] string? end)
        {
            var report = LoadReport();

            SetText(report, "Title", "Raport Testów");
            SetText(report, "Panel1Header", "Zakres dat");
            SetText(report, "Panel1Body", $"Od: {start} Do: {end}");

            SetText(report, "Panel2Header", "Status");
            SetText(report, "Panel2Body", "Wydajność systemu testowego w zadanym okresie.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            
            table.Rows.Add("Egzamin SQL", "2025-05-10");
            table.Rows.Add("Kolokwium C#", "2025-06-01");
            table.Rows.Add("Test bezpieczeństwo", "2025-06-15");

            return ExportReport(report, table, "testy.pdf");
        }

        // ==========================================
        // 4. RAPORT: Karta egzaminacyjna (Formularz)
        // ==========================================
        [HttpGet("test-form")]
        public IActionResult GetTestForm([FromQuery] int? id_testu, [FromQuery] int? id_uzytkownika)
        {
            var report = LoadReport();

            SetText(report, "Title", "KARTA EGZAMINACYJNA");
            SetText(report, "Panel1Header", "Dane Studenta");
            SetText(report, "Panel1Body", $"ID Studenta: {id_uzytkownika}, ID Testu: {id_testu}");

            SetText(report, "Panel2Header", "Wynik");
            SetText(report, "Panel2Body", "Egzamin zaliczony pozytywnie.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));

            table.Rows.Add("Pytanie 1: Co to SQL?", "Poprawna");
            table.Rows.Add("Pytanie 2: Co to JOIN?", "Błędna");
            table.Rows.Add("Pytanie 3: Klucz główny?", "Poprawna");

            return ExportReport(report, table, "karta.pdf");
        }


        // ==========================================
        // METODY POMOCNICZE
        // ==========================================

        private Report LoadReport()
        {
            var report = new Report();
            // Ładujemy Twój szablon
            report.Load("Reports/raport_testow.frx");
            return report;
        }

        // Pomocnicza funkcja do zmiany tekstu w polach TextObject (np. Title)
        private void SetText(Report report, string objectName, string text)
        {
            var obj = report.FindObject(objectName) as FastReport.TextObject;
            if (obj != null)
            {
                obj.Text = text;
                // FIX DLA LINUXA: Zmieniamy czcionkę na Arial, bo Montserrat może nie istnieć
                obj.Font = new System.Drawing.Font("Arial", obj.Font.Size, obj.Font.Style);
            }
        }

        // Główna funkcja generująca PDF z danymi
        private IActionResult ExportReport(Report report, DataTable data, string fileName)
        {
            // Rejestrujemy dane w raporcie. 
            // WAZNE: W pliku .frx musisz mieć DataBand podpięty do źródła o nazwie "Dane"
            // Ale nawet bez tego, powyższe podmiany SetText zadziałają.
            report.RegisterData(data, "Dane");

            // Jeśli w raporcie masz DataBand, spróbujmy go podpiąć kodem (opcjonalne, ale pomaga)
            var dataBand = report.FindObject("ListBand") as FastReport.DataBand;
            if (dataBand != null)
            {
                dataBand.DataSource = report.GetDataSource("Dane");
            }
            
            // Podmiana tekstu w liście, żeby korzystał z danych [Dane.Nazwa]
            // Uwaga: To zadziała w prosty sposób, jeśli w .frx masz TextObject o nazwie "ListItem"
            var listItem = report.FindObject("ListItem") as FastReport.TextObject;
            if(listItem != null)
            {
                listItem.Text = "[Dane.Nazwa]: [Dane.Wartosc]";
                listItem.Font = new System.Drawing.Font("Arial", 10);
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
