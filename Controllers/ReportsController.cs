using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using FastReport.Table;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Drawing; 
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace FastReportService.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
        // =========================================================
        // ENDPOINTY
        // =========================================================
        
        [HttpPost("questions-stats")]
        public IActionResult GenerateStats([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "RAPORT STATYSTYK", "Dane wygenerowane z bazy");
        }

        [HttpPost("users")] 
        [HttpPost("students-list")] // Obsługa obu nazw
        public IActionResult GenerateUsers([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "LISTA STUDENTÓW", "Aktualny wykaz osób w systemie");
        }

        [HttpPost("exam-results")]
        public IActionResult GenerateExam([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "WYNIKI EGZAMINU", "Protokół wyników testu");
        }

        [HttpPost("questions-bank")]
        public IActionResult GenerateQuestions([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "BANK PYTAŃ", "Wykaz pytań z bazy");
        }

        [HttpPost("tests-stats")]
        public IActionResult GenerateTestStats([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "STATYSTYKA TESTÓW", "Zestawienie zdawalności");
        }

        // =========================================================
        // SILNIK GENEROWANIA
        // =========================================================
        
        private IActionResult GeneratePdfFromData(List<Dictionary<string, object>> jsonData, string title, string subtitle)
        {
            try 
            {
                var report = new Report();
                
                // Ładowanie szablonu (z fallbackiem ścieżki)
                string reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "raport_testow.frx");
                if (!System.IO.File.Exists(reportPath)) reportPath = "Reports/raport_testow.frx";
                
                report.Load(reportPath);
                
                // Naprawa fontów
                try { FixReportVisuals(report); } catch { }

                // Nagłówki
                SetText(report, "Title", title);
                SetText(report, "Panel1Header", subtitle);
                SetText(report, "Panel1Body", $"Data: {DateTime.Now:yyyy-MM-dd HH:mm}");

                // Dane
                var table = JsonToDataTable(jsonData);

                // Rejestracja danych
                report.RegisterData(table, "Dane");
                report.GetDataSource("Dane").Enabled = true;

                // Czyszczenie starego szablonu
                DataBand dataBand = report.FindObject("ListBand") as DataBand;
                if (dataBand != null)
                {
                    dataBand.Objects.Clear();
                    dataBand.DataSource = report.GetDataSource("Dane");
                }

                // Generowanie tabeli lub komunikatu
                if (table.Rows.Count > 0)
                {
                    BuildDynamicTable(report, table);
                }
                else
                {
                    if (dataBand != null)
                    {
                        var noDataText = new FastReport.TextObject();
                        noDataText.Parent = dataBand;
                        noDataText.Bounds = new RectangleF(0, 0, 700, 30);
                        noDataText.Text = "BRAK DANYCH DO WYŚWIETLENIA";
                        noDataText.Font = new Font("DejaVu Sans", 12, FontStyle.Bold);
                        noDataText.TextFill = new SolidFill(Color.Red);
                        noDataText.HorzAlign = HorzAlign.Center;
                    }
                }

                report.Prepare();

                using var ms = new MemoryStream();
                var pdf = new PDFSimpleExport();
                report.Export(pdf, ms);
                ms.Position = 0;

                return File(ms.ToArray(), "application/pdf", "raport.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        // =========================================================
        // METODY POMOCNICZE
        // =========================================================

        private DataTable JsonToDataTable(List<Dictionary<string, object>> list)
        {
            DataTable table = new DataTable("Dane");
            if (list == null || list.Count == 0) return table;

            // Tworzenie kolumn (bezpieczne nazwy bez spacji)
            foreach (var key in list[0].Keys)
            {
                string safeColumnName = key.Replace(" ", "_").Replace(".", "");
                table.Columns.Add(safeColumnName, typeof(string));
            }

            foreach (var item in list)
            {
                var row = table.NewRow();
                int colIndex = 0;
                foreach (var key in item.Keys)
                {
                    object val = item[key];
                    if (val == null) row[colIndex] = "";
                    else if (val is JsonElement je) row[colIndex] = je.ToString();
                    else row[colIndex] = val.ToString();
                    colIndex++;
                }
                table.Rows.Add(row);
            }
            return table;
        }
        
        private void BuildDynamicTable(Report report, DataTable data)
        {
            DataBand dataBand = report.FindObject("ListBand") as DataBand;
            if (dataBand == null) return;

            TableObject table = new TableObject();
            table.Name = "DynamicTable_" + Guid.NewGuid().ToString().Replace("-", "");
            table.Parent = dataBand;
            table.Width = 700; 
            table.Height = 25;
            
            // Konfiguracja wymiarów tabeli
            table.ColumnCount = data.Columns.Count;
            table.RowCount = 1; 
            
            float colWidth = 700f / (float)data.Columns.Count;

            for (int i = 0; i < data.Columns.Count; i++)
            {
                // Ustawienie szerokości kolumny
                table.Columns[i].Width = colWidth;

                // --- TU BYŁ BŁĄD ---
                // Poprawne indeksowanie: table[kolumna, wiersz]
                // Wcześniej było table[0, i] (błędne), teraz jest table[i, 0] (poprawne)
                TableCell cell = table[i, 0]; 
                
                // Zabezpieczenie na wypadek, gdyby komórka nie istniała
                if (cell == null)
                {
                    cell = new TableCell();
                    cell.Parent = table.Rows[0];
                }
                
                string colName = data.Columns[i].ColumnName;
                cell.Text = $"[Dane.{colName}]";
                
                cell.Font = new Font("DejaVu Sans", 9, FontStyle.Regular);
                cell.TextFill = new SolidFill(Color.Black);
                cell.Border.Lines = BorderLines.All;
                cell.Border.Color = Color.Black;
                cell.VertAlign = VertAlign.Center;
                cell.HorzAlign = HorzAlign.Center;
            }
            
            // Nagłówki w Panel2 (opcjonalnie)
            var headerObj = report.FindObject("Panel2Header") as FastReport.TextObject;
            if (headerObj != null)
            {
                var headers = data.Columns.Cast<DataColumn>()
                                  .Select(c => c.ColumnName.Replace("_", " "))
                                  .ToArray();
                headerObj.Text = string.Join(" | ", headers);
                headerObj.Font = new Font("DejaVu Sans", 9, FontStyle.Bold);
                headerObj.TextFill = new SolidFill(Color.Black);
            }
            var p2b = report.FindObject("Panel2Body") as FastReport.TextObject;
            if(p2b != null) p2b.Text = "";
        }

        private void FixReportVisuals(Report report)
        {
            string fontName = "DejaVu Sans"; 
            foreach (Base obj in report.AllObjects)
            {
                if (obj is FastReport.TextObject textObj)
                {
                    textObj.Font = new Font(fontName, textObj.Font.Size, textObj.Font.Style);
                    textObj.TextFill = new SolidFill(Color.Black);
                }
                if (obj is FastReport.ShapeObject shapeObj) shapeObj.Fill = new SolidFill(Color.LightGray);
            }
        }

        private void SetText(Report report, string objectName, string text)
        {
            var obj = report.FindObject(objectName) as FastReport.TextObject;
            if (obj != null) obj.Text = text;
        }
    }
}
