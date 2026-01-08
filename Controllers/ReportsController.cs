using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Drawing; // Do klasy Font i Color
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
            var report = LoadAndFixReport();

            SetText(report, "Title", "RAPORT STATYSTYK");
            SetText(report, "Panel1Header", "Kryteria");
            SetText(report, "Panel1Body", $"Bank: {id_banku}, Kat: {id_kategorii}");
            SetText(report, "Panel2Header", "Info");
            SetText(report, "Panel2Body", "Raport wygenerowany automatycznie.");

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Latwe", "15");
            table.Rows.Add("Trudne", "8");

            return ExportReport(report, table, "statystyki.pdf");
        }

        // ==========================================
        // 2. RAPORT: Lista użytkowników
        // ==========================================
        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] string? rola, [FromQuery] string? email)
        {
            var report = LoadAndFixReport();
            SetText(report, "Title", "LISTA UZYTKOWNIKOW");
            SetText(report, "Panel1Header", "Filtry");
            SetText(report, "Panel1Body", $"Rola: {rola}");
            
            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Jan Kowalski", "Student");
            
            return ExportReport(report, table, "uzytkownicy.pdf");
        }
        
        // ==========================================
        // 3. RAPORT: Testy pogrupowane
        // ==========================================
        [HttpGet("tests-grouped")]
        public IActionResult GetTestsGrouped([FromQuery] string? start, [FromQuery] string? end)
        {
            var report = LoadAndFixReport();
            SetText(report, "Title", "RAPORT TESTOW");
            SetText(report, "Panel1Header", "Zakres dat");
            SetText(report, "Panel1Body", $"Od: {start} Do: {end}");
            SetText(report, "Panel2Header", "Status");
            SetText(report, "Panel2Body", "Wydajnosc systemu."); // Bez polskich znaków w kodzie dla bezpieczeństwa

            var table = new DataTable("Dane");
            table.Columns.Add("Nazwa", typeof(string));
            table.Columns.Add("Wartosc", typeof(string));
            table.Rows.Add("Test SQL", "2025");
            return ExportReport(report, table, "testy.pdf");
        }

        // ==========================================
        // 4. RAPORT: Karta egzaminacyjna
        // ==========================================
        [HttpGet("test-form")]
        public IActionResult GetTestForm([FromQuery] int? id_testu, [FromQuery] int? id_uzytkownika)
        {
            var report = LoadAndFixReport();
            SetText(report, "Title", "KARTA EGZAMINU");
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
        // METODY POMOCNICZE (TUTAJ BYŁ BŁĄD)
        // ==========================================

        private Report LoadAndFixReport()
        {
            var report = new Report();
            report.Load("Reports/raport_testow.frx");
            
            FixReportVisuals(report); 
            
            return report;
        }

        private void FixReportVisuals(Report report)
        {
            // Używamy czcionki systemowej dostępnej w Dockerze (zainstalowanej w Dockerfile)
            string fontName = "DejaVu Sans"; 

            foreach (Base obj in report.AllObjects)
            {
                // 1. NAPRAWA TEKSTU: Zmień czcionkę na DejaVu ORAZ kolor na CZARNY
                if (obj is FastReport.TextObject textObj)
                {
                    textObj.Font = new Font(fontName, textObj.Font.Size, textObj.Font.Style);
                    
                    // --- POPRAWKA: Używamy SolidFill zamiast SolidBrush ---
                    textObj.TextFill = new SolidFill(Color.Black); 
                }
                
                // 2. NAPRAWA TŁA: Jeśli tło ma przezroczystość, usuń ją (Linux tego nie lubi)
                if (obj is FastReport.ShapeObject shapeObj)
                {
                    // Zmieniamy na jasnoszare, pełne tło (bez Blend)
                    // Tutaj SolidFill było poprawne
                    shapeObj.Fill = new SolidFill(Color.LightGray);
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
            if(listItem != null) 
            {
                listItem.Text = "[Dane.Nazwa]: [Dane.Wartosc]";
                
                // --- POPRAWKA: Używamy SolidFill zamiast SolidBrush ---
                listItem.TextFill = new SolidFill(Color.Black);
                
                listItem.Font = new Font("DejaVu Sans", 10, FontStyle.Regular);
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
