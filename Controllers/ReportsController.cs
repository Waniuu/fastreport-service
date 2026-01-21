using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using FastReport.Table;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FastReportService.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
        // ... pozostałe endpointy (questions-stats, users, itd.)

        [HttpPost("questions-bank")]
        public IActionResult GenerateQuestions([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "BANK PYTAŃ", "Wykaz pytań z bazy");
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

                SetText(report, "Title", title);
                SetText(report, "Panel1Header", $"Raport wygenerowany: {DateTime.Now:dd.MM.yyyy, HH:mm}");
                SetText(report, "Panel1Body", subtitle);
                
                HideUnusedObjects(report);

                var table = JsonToDataTable(jsonData);
                report.RegisterData(table, "Dane");

                // PRZYGO TUJ DANE DLA WYKRESÓW
                if (title == "BANK PYTAŃ" && jsonData.Any())
                {
                    // Dane dla wykresu kategorii
                    var categoryData = PrepareChartDataCategory(jsonData);
                    report.RegisterData(categoryData, "ChartData_Category");
                    
                    // Dane dla wykresu poziomu trudności
                    var levelData = PrepareChartDataLevel(jsonData);
                    report.RegisterData(levelData, "ChartData_Level");
                }

                // Przygotuj tabelę z danymi
                DataBand? dataBand = report.FindObject("ListBand") as DataBand;
                if (dataBand != null)
                {
                    BuildTableForQuestionsBank(dataBand, table);
                }

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
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // =========================================================
        // NOWE METODY DLA WYKRESÓW
        // =========================================================

        private DataTable PrepareChartDataCategory(List<Dictionary<string, object>> jsonData)
        {
            var grouped = jsonData
                .GroupBy(d => d.ContainsKey("Kategoria") ? d["Kategoria"]?.ToString() ?? "Nieokreślona" : "Nieokreślona")
                .Select(g => new { Kategoria = g.Key, Ilosc = g.Count() })
                .ToList();

            DataTable table = new DataTable("ChartData_Category");
            table.Columns.Add("Kategoria", typeof(string));
            table.Columns.Add("Ilosc", typeof(int));

            foreach (var item in grouped)
            {
                table.Rows.Add(item.Kategoria, item.Ilosc);
            }

            return table;
        }

        private DataTable PrepareChartDataLevel(List<Dictionary<string, object>> jsonData)
        {
            var grouped = jsonData
                .GroupBy(d => d.ContainsKey("Poziom") ? d["Poziom"]?.ToString() ?? "Nieokreślony" : "Nieokreślony")
                .Select(g => new { Poziom = g.Key, Ilosc = g.Count() })
                .ToList();

            DataTable table = new DataTable("ChartData_Level");
            table.Columns.Add("Poziom", typeof(string));
            table.Columns.Add("Ilosc", typeof(int));

            foreach (var item in grouped)
            {
                table.Rows.Add(item.Poziom, item.Ilosc);
            }

            return table;
        }

        private void BuildTableForQuestionsBank(DataBand dataBand, DataTable data)
        {
            // Usuń istniejący obiekt ListItem jeśli istnieje
            var listItem = report.FindObject("ListItem") as FastReport.TextObject;
            if (listItem != null) listItem.Dispose();

            // Ukryj DataBand i utwórz tabelę statycznie
            dataBand.Visible = false;

            // Tworzenie tabeli dynamicznej
            TableObject table = new TableObject();
            table.Name = "DynamicTable";
            table.Parent = dataBand.Parent; // Dodaj bezpośrednio na stronę
            table.Left = 0;
            table.Top = 400; // Poniżej wykresów
            table.Width = 680;
            
            float headerHeight = 35f;
            float rowHeight = 25f;
            table.Height = headerHeight + (data.Rows.Count * rowHeight);

            table.ColumnCount = data.Columns.Count;
            table.RowCount = data.Rows.Count + 1;

            // Stylizacja
            Color headerBackColor = Color.FromArgb(41, 58, 74);
            Color headerTextColor = Color.White;
            Color rowAltColor = Color.FromArgb(240, 242, 245);
            Color borderColor = Color.FromArgb(200, 200, 200);

            for (int i = 0; i < data.Columns.Count; i++)
            {
                table.Columns[i].Width = 680 / data.Columns.Count;

                // Nagłówek
                TableCell headerCell = table[i, 0];
                headerCell.Text = data.Columns[i].ColumnName.ToUpper();
                headerCell.Font = new Font("Arial", 10, FontStyle.Bold);
                headerCell.TextFill = new SolidFill(headerTextColor);
                headerCell.Fill = new SolidFill(headerBackColor);
                headerCell.Border.Lines = BorderLines.All;
                headerCell.Border.Color = borderColor;
                headerCell.HorzAlign = HorzAlign.Center;
                headerCell.VertAlign = VertAlign.Center;

                // Wiersze danych
                for (int row = 0; row < data.Rows.Count; row++)
                {
                    TableCell dataCell = table[i, row + 1];
                    string cellValue = data.Rows[row][i]?.ToString() ?? "";
                    dataCell.Text = cellValue;
                    
                    dataCell.Font = new Font("Arial", 9, FontStyle.Regular);
                    dataCell.TextFill = new SolidFill(Color.Black);

                    if (row % 2 != 0)
                        dataCell.Fill = new SolidFill(rowAltColor);
                    else
                        dataCell.Fill = new SolidFill(Color.White);

                    dataCell.Border.Lines = BorderLines.Bottom | BorderLines.Right | BorderLines.Left;
                    dataCell.Border.Color = borderColor;
                    dataCell.VertAlign = VertAlign.Center;
                    dataCell.HorzAlign = HorzAlign.Left;
                }
            }

            table.RepeatHeaders = true;
        }

        // =========================================================
        // METODY POMOCNICZE (pozostałe bez zmian)
        // =========================================================

        private DataTable JsonToDataTable(List<Dictionary<string, object>> list)
        {
            DataTable table = new DataTable("Dane");
            if (list == null || list.Count == 0) return table;

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
                    row[colIndex] = item[key]?.ToString() ?? "";
                    colIndex++;
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private void FixReportVisuals(Report report)
        {
            foreach (Base obj in report.AllObjects)
            {
                if (obj is FastReport.TextObject textObj)
                {
                    textObj.Font = new Font("Arial", textObj.Font.Size, textObj.Font.Style);
                    textObj.TextFill = new SolidFill(Color.Black);
                }
            }
        }

        private void HideUnusedObjects(Report report)
        {
            var objectsToHide = new[] { "Panel2", "Panel2Header", "Panel2Body", "ListHeader" };
            foreach (var objName in objectsToHide)
            {
                var obj = report.FindObject(objName);
                if (obj != null) obj.Visible = false;
            }
        }

        private void CreatePageFooter(Report report)
        {
            var page = report.Pages[0] as ReportPage;
            if (page == null) return;

            var existingFooter = report.FindObject("PageFooter") as FastReport.TextObject;
            if (existingFooter != null) return;

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
            }
        }
    }
}
