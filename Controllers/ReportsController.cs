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
        [HttpPost("students-list")]
        public IActionResult GenerateUsers([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "Lista Studentów", "Aktualny wykaz osób w systemie");
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
                
                string reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "raport_testow.frx");
                if (!System.IO.File.Exists(reportPath)) reportPath = "Reports/raport_testow.frx";
                
                report.Load(reportPath);
                
                try { FixReportVisuals(report); } catch { }

                // Ustawienie tekstu w istniejących obiektach z szablonu
                SetText(report, "Title", title);
                SetText(report, "Panel1Header", "Raport wygenerowany: " + DateTime.Now.ToString("dd.MM.yyyy, HH:mm:ss"));
                SetText(report, "Panel1Body", subtitle);
                
                // Ukryj niepotrzebne elementy z szablonu
                HideUnusedObjects(report);

                var table = JsonToDataTable(jsonData);
                report.RegisterData(table, "Dane");
                report.GetDataSource("Dane").Enabled = true;

                DataBand? dataBand = report.FindObject("ListBand") as DataBand;
                if (dataBand != null)
                {
                    // Usuń istniejący obiekt ListItem z szablonu
                    var listItem = report.FindObject("ListItem") as FastReport.TextObject;
                    if (listItem != null) listItem.Dispose();
                    
                    dataBand.DataSource = report.GetDataSource("Dane");
                }

                if (table.Rows.Count > 0)
                {
                    BuildDynamicTable(report, table);
                }
                else
                {
                    if (dataBand != null)
                    {
                        var noDataText = new FastReport.TextObject();
                        noDataText.Name = "NoDataText";
                        noDataText.Parent = dataBand;
                        noDataText.Bounds = new RectangleF(0, 0, 680, 30);
                        noDataText.Text = "BRAK DANYCH DO WYŚWIETLENIA";
                        noDataText.Font = new Font("Arial", 12, FontStyle.Bold);
                        noDataText.TextFill = new SolidFill(Color.Red);
                        noDataText.HorzAlign = HorzAlign.Center;
                    }
                }

                // Utwórz stopkę z numeracją stron
                CreatePageFooter(report);

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

            // Tworzenie kolumn z oryginalnymi nazwami
            foreach (var key in list[0].Keys)
            {
                table.Columns.Add(key, typeof(string));
            }

            foreach (var item in list)
            {
                var row = table.NewRow();
                int colIndex = 0;
                foreach (var key in item.Keys)
                {
                    object val = item[key];
                    row[colIndex] = val?.ToString() ?? "";
                    colIndex++;
                }
                table.Rows.Add(row);
            }
            return table;
        }
        
        private void BuildDynamicTable(Report report, DataTable data)
        {
            DataBand? dataBand = report.FindObject("ListBand") as DataBand;
            if (dataBand == null) return;

            // Utwórz tabelę z nagłówkiem i danymi
            TableObject table = new TableObject();
            table.Name = "DynamicTable";
            table.Parent = dataBand;
            table.Width = 680; // Dopasuj do szerokości szablonu
            table.Height = 25 * (data.Rows.Count + 1); // Wysokość: nagłówek + wiersze danych
            
            table.ColumnCount = data.Columns.Count;
            table.RowCount = data.Rows.Count + 1; // +1 dla nagłówka
            
            // Oblicz szerokości kolumn
            float[] columnWidths = CalculateColumnWidths(data, 680);
            for (int i = 0; i < data.Columns.Count; i++)
            {
                table.Columns[i].Width = columnWidths[i];
                
                // Nagłówek tabeli (wiersz 0)
                TableCell headerCell = table[i, 0];
                if (headerCell == null)
                {
                    headerCell = new TableCell();
                    headerCell.Parent = table.Rows[0];
                }
                
                string colName = data.Columns[i].ColumnName;
                headerCell.Text = colName.ToUpper();
                headerCell.Font = new Font("Arial", 10, FontStyle.Bold);
                headerCell.TextFill = new SolidFill(Color.Black);
                headerCell.Border.Lines = BorderLines.All;
                headerCell.Border.Color = Color.Gray;
                headerCell.Fill = new SolidFill(Color.LightGray);
                headerCell.VertAlign = VertAlign.Center;
                headerCell.HorzAlign = HorzAlign.Center;
                headerCell.Padding = new FastReport.Utils.Padding(3, 3, 3, 3); // Poprawione: wszystkie 4 wartości
                
                // Wiersze danych
                for (int row = 0; row < data.Rows.Count; row++)
                {
                    TableCell dataCell = table[i, row + 1];
                    if (dataCell == null)
                    {
                        dataCell = new TableCell();
                        dataCell.Parent = table.Rows[row + 1];
                    }
                    
                    dataCell.Text = $"[Dane.{colName}]";
                    dataCell.Font = new Font("Arial", 9, FontStyle.Regular);
                    dataCell.TextFill = new SolidFill(Color.Black);
                    dataCell.Border.Lines = BorderLines.All;
                    dataCell.Border.Color = Color.LightGray;
                    dataCell.VertAlign = VertAlign.Center;
                    dataCell.HorzAlign = HorzAlign.Left;
                    dataCell.Padding = new FastReport.Utils.Padding(3, 3, 3, 3); // Poprawione: wszystkie 4 wartości
                }
            }
            
            // Ustaw, aby nagłówek powtarzał się na każdej stronie
            table.RepeatHeaders = true;
        }

        private float[] CalculateColumnWidths(DataTable data, float totalWidth)
        {
            float[] widths = new float[data.Columns.Count];
            
            // Domyślne proporcje dla typowych kolumn
            Dictionary<string, float> columnRatios = new Dictionary<string, float>
            {
                { "lp", 0.5f },
                { "id", 0.7f },
                { "l.p", 0.5f },
                { "numer", 0.5f },
                { "imie", 1.2f },
                { "nazwisko", 1.5f },
                { "email", 2.0f },
                { "status", 1.0f },
                { "wynik", 1.0f },
                { "data", 1.2f },
                { "ocena", 0.8f }
            };
            
            float totalRatio = 0;
            for (int i = 0; i < data.Columns.Count; i++)
            {
                string colName = data.Columns[i].ColumnName.ToLower();
                float ratio = 1.0f;
                
                foreach (var key in columnRatios.Keys)
                {
                    if (colName.Contains(key))
                    {
                        ratio = columnRatios[key];
                        break;
                    }
                }
                
                widths[i] = ratio;
                totalRatio += ratio;
            }
            
            // Przelicz na rzeczywiste szerokości
            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] = (widths[i] / totalRatio) * totalWidth;
            }
            
            return widths;
        }

        private void FixReportVisuals(Report report)
        {
            string fontName = "Arial";
            foreach (Base obj in report.AllObjects)
            {
                if (obj is FastReport.TextObject textObj)
                {
                    textObj.Font = new Font(fontName, textObj.Font.Size, textObj.Font.Style);
                    textObj.TextFill = new SolidFill(Color.Black);
                }
                if (obj is FastReport.ShapeObject shapeObj) 
                    shapeObj.Fill = new SolidFill(Color.Transparent);
                    
                if (obj is FastReport.PictureObject pictureObj && pictureObj.Name == "HeaderImage")
                {
                    // Ukryj obrazek nagłówka, jeśli nie jest potrzebny
                    pictureObj.Visible = false;
                }
            }
        }

        private void HideUnusedObjects(Report report)
        {
            // Ukryj Panel2, który nie jest potrzebny w naszym przypadku
            var panel2 = report.FindObject("Panel2") as FastReport.ShapeObject;
            if (panel2 != null) panel2.Visible = false;
            
            var panel2Header = report.FindObject("Panel2Header") as FastReport.TextObject;
            if (panel2Header != null) panel2Header.Visible = false;
            
            var panel2Body = report.FindObject("Panel2Body") as FastReport.TextObject;
            if (panel2Body != null) panel2Body.Visible = false;
            
            // Ukryj ListHeader z szablonu
            var listHeader = report.FindObject("ListHeader") as FastReport.TextObject;
            if (listHeader != null) listHeader.Visible = false;
        }

        private void CreatePageFooter(Report report)
        {
            // Znajdź stronę raportu
            var page = report.Pages[0] as ReportPage;
            if (page == null) return;

            // Utwórz stopkę
            var pageFooter = new FastReport.TextObject();
            pageFooter.Name = "PageFooter";
            pageFooter.Parent = page;
            pageFooter.Bounds = new RectangleF(0, page.Height - 30, page.Width, 20);
            pageFooter.Text = "Strona [Page#] z [TotalPages#]";
            pageFooter.Font = new Font("Arial", 8, FontStyle.Regular);
            pageFooter.HorzAlign = HorzAlign.Right;
            pageFooter.VertAlign = VertAlign.Bottom;
            pageFooter.TextFill = new SolidFill(Color.Black);
        }

        private void SetText(Report report, string objectName, string text)
        {
            var obj = report.FindObject(objectName) as FastReport.TextObject;
            if (obj != null) 
            {
                obj.Text = text;
                
                // Styl dla tytułu
                if (objectName == "Title")
                {
                    obj.Font = new Font("Arial", 16, FontStyle.Bold);
                    obj.HorzAlign = HorzAlign.Center;
                    obj.TextFill = new SolidFill(Color.Black);
                }
                
                // Styl dla daty wygenerowania
                if (objectName == "Panel1Header")
                {
                    obj.Font = new Font("Arial", 10, FontStyle.Regular);
                    obj.HorzAlign = HorzAlign.Left;
                }
                
                // Styl dla podtytułu
                if (objectName == "Panel1Body")
                {
                    obj.Font = new Font("Arial", 12, FontStyle.Bold);
                    obj.HorzAlign = HorzAlign.Left;
                }
            }
        }
    }
}
