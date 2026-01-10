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

                // Ustawienie nagłówków zgodnie z przykladem.pdf
                SetText(report, "SystemHeader", "System Generowania Testów");
                SetText(report, "Title", title);
                SetText(report, "Panel1Header", "Raport wygenerowany: " + DateTime.Now.ToString("dd.MM.yyyy, HH:mm:ss"));
                SetText(report, "Panel1Body", subtitle);

                var table = JsonToDataTable(jsonData);
                report.RegisterData(table, "Dane");
                report.GetDataSource("Dane").Enabled = true;

                DataBand dataBand = report.FindObject("ListBand") as DataBand;
                if (dataBand != null)
                {
                    dataBand.Objects.Clear();
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
                        noDataText.Parent = dataBand;
                        noDataText.Bounds = new RectangleF(0, 0, 700, 30);
                        noDataText.Text = "BRAK DANYCH DO WYŚWIETLENIA";
                        noDataText.Font = new Font("Arial", 12, FontStyle.Bold);
                        noDataText.TextFill = new SolidFill(Color.Red);
                        noDataText.HorzAlign = HorzAlign.Center;
                    }
                }

                // Dodanie stopki z numeracją stron
                var pageFooter = report.FindObject("PageFooter") as FastReport.TextObject;
                if (pageFooter != null)
                {
                    pageFooter.Text = "Strona [Page#] z [TotalPages#]";
                    pageFooter.Font = new Font("Arial", 8, FontStyle.Regular);
                    pageFooter.HorzAlign = HorzAlign.Right;
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

            // Tworzenie kolumn z oryginalnymi nazwami (bez zamiany spacji)
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
            DataBand dataBand = report.FindObject("ListBand") as DataBand;
            if (dataBand == null) return;

            // Tworzenie tabeli z nagłówkiem i wierszem danych
            TableObject table = new TableObject();
            table.Name = "DynamicTable_" + Guid.NewGuid().ToString().Replace("-", "");
            table.Parent = dataBand;
            table.Width = 700;
            table.Height = 40; // Większa wysokość dla lepszego wyglądu
            
            table.ColumnCount = data.Columns.Count;
            table.RowCount = 2; // Dwa wiersze: nagłówek i dane
            
            // Ustawienie automatycznych szerokości kolumn
            float[] columnWidths = CalculateColumnWidths(data);
            for (int i = 0; i < data.Columns.Count; i++)
            {
                table.Columns[i].Width = columnWidths[i];
                
                // Nagłówek tabeli
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
                headerCell.Padding = new System.Windows.Forms.Padding(3);
                
                // Komórka z danymi
                TableCell dataCell = table[i, 1];
                if (dataCell == null)
                {
                    dataCell = new TableCell();
                    dataCell.Parent = table.Rows[1];
                }
                
                dataCell.Text = $"[Dane.{colName}]";
                dataCell.Font = new Font("Arial", 9, FontStyle.Regular);
                dataCell.TextFill = new SolidFill(Color.Black);
                dataCell.Border.Lines = BorderLines.All;
                dataCell.Border.Color = Color.LightGray;
                dataCell.VertAlign = VertAlign.Center;
                dataCell.HorzAlign = HorzAlign.Left;
                dataCell.Padding = new System.Windows.Forms.Padding(3);
            }
            
            // Ustawienie, aby nagłówek powtarzał się na każdej stronie
            table.RepeatHeaders = true;
            
            // Wyczyść stare nagłówki
            var headerObj = report.FindObject("Panel2Header") as FastReport.TextObject;
            if (headerObj != null) headerObj.Text = "";
            var p2b = report.FindObject("Panel2Body") as FastReport.TextObject;
            if(p2b != null) p2b.Text = "";
        }

        private float[] CalculateColumnWidths(DataTable data)
        {
            float totalWidth = 700f;
            float[] widths = new float[data.Columns.Count];
            
            // Domyślne proporcje dla typowych kolumn
            Dictionary<string, float> defaultRatios = new Dictionary<string, float>
            {
                { "lp", 0.5f },
                { "id", 0.7f },
                { "email", 1.5f },
                { "data", 1.2f },
                { "status", 0.8f }
            };
            
            float totalRatio = 0;
            for (int i = 0; i < data.Columns.Count; i++)
            {
                string colName = data.Columns[i].ColumnName.ToLower();
                float ratio = 1.0f; // Domyślna proporcja
                
                foreach (var key in defaultRatios.Keys)
                {
                    if (colName.Contains(key))
                    {
                        ratio = defaultRatios[key];
                        break;
                    }
                }
                
                widths[i] = ratio;
                totalRatio += ratio;
            }
            
            // Przeliczenie na rzeczywiste szerokości
            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] = (widths[i] / totalRatio) * totalWidth;
            }
            
            return widths;
        }

        private void FixReportVisuals(Report report)
        {
            string fontName = "Arial"; // Zmiana na Arial dla lepszej czytelności
            foreach (Base obj in report.AllObjects)
            {
                if (obj is FastReport.TextObject textObj)
                {
                    textObj.Font = new Font(fontName, textObj.Font.Size, textObj.Font.Style);
                    textObj.TextFill = new SolidFill(Color.Black);
                }
                if (obj is FastReport.ShapeObject shapeObj) 
                    shapeObj.Fill = new SolidFill(Color.LightGray);
                if (obj is FastReport.TableObject tableObj)
                {
                    foreach (TableRow row in tableObj.Rows)
                    {
                        foreach (TableCell cell in row)
                        {
                            if (cell != null)
                            {
                                cell.Font = new Font(fontName, cell.Font.Size, cell.Font.Style);
                            }
                        }
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
                // Ustawienie stylu dla tytułów
                if (objectName == "Title")
                {
                    obj.Font = new Font("Arial", 16, FontStyle.Bold);
                    obj.HorzAlign = HorzAlign.Center;
                }
                else if (objectName == "SystemHeader")
                {
                    obj.Font = new Font("Arial", 12, FontStyle.Bold);
                    obj.TextFill = new SolidFill(Color.DarkBlue);
                    obj.HorzAlign = HorzAlign.Center;
                }
            }
        }
    }
}
