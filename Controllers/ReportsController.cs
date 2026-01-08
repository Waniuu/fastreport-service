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

namespace FastReportService.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
        // 1. LISTA STUDENTÓW
        [HttpPost("students-list")]
        public IActionResult ReportStudents([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "LISTA STUDENTÓW", "Aktualny wykaz osób w systemie");
        }

        // 2. WYNIKI EGZAMINU (Dla konkretnego testu)
        [HttpPost("exam-results")]
        public IActionResult ReportExam([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "PROTOKÓŁ EGZAMINACYJNY", "Zestawienie wyników dla wybranego testu");
        }

        // 3. BANK PYTAŃ
        [HttpPost("questions-bank")]
        public IActionResult ReportQuestions([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "BANK PYTAŃ", "Wykaz pytań wg kategorii i trudności");
        }

        // 4. STATYSTYKA TESTÓW
        [HttpPost("tests-stats")]
        public IActionResult ReportStats([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "STATYSTYKA ZDAWALNOŚCI", "Analiza wyników i popularności testów");
        }

        // =========================================================
        // SILNIK GENEROWANIA TABELI (Bezpieczny dla Linuxa)
        // =========================================================
        
        private IActionResult GeneratePdfFromData(List<Dictionary<string, object>> jsonData, string title, string subtitle)
        {
            try 
            {
                var report = new Report();
                report.Load("Reports/raport_testow.frx");
                
                FixReportVisuals(report); // Fonty i kolory
                
                // Ustawiamy nagłówki
                SetText(report, "Title", title);
                SetText(report, "Panel1Header", subtitle);
                SetText(report, "Panel1Body", $"Data generowania: {DateTime.Now:yyyy-MM-dd HH:mm}");

                // JSON -> DataTable
                var table = JsonToDataTable(jsonData);

                // Czyścimy szablon
                DataBand dataBand = report.FindObject("ListBand") as DataBand;
                if (dataBand != null) dataBand.Objects.Clear();

                // Rysujemy tabelę LUB komunikat o braku danych
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
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private void BuildDynamicTable(Report report, DataTable data)
        {
            report.RegisterData(data, "Dane");
            DataBand dataBand = report.FindObject("ListBand") as DataBand;
            if (dataBand == null) return;

            dataBand.Objects.Clear();
            dataBand.DataSource = report.GetDataSource("Dane");

            TableObject table = new TableObject();
            table.Name = "DynamicTable";
            table.Parent = dataBand;
            table.Width = 700; 
            table.Height = 25;
            
            table.ColumnCount = data.Columns.Count;
            table.RowCount = 1; 
            
            float colWidth = 700f / data.Columns.Count;

            for (int i = 0; i < data.Columns.Count; i++)
            {
                table.Columns[i].Width = colWidth;
                TableCell cell = table[0, i];
                
                // Bindowanie danych
                cell.Text = $"[Dane.{data.Columns[i].ColumnName}]";
                
                // Stylizacja bezpieczna dla Linuxa
                cell.Font = new Font("DejaVu Sans", 9, FontStyle.Regular);
                cell.TextFill = new SolidFill(Color.Black);
                cell.Border.Lines = BorderLines.All;
                cell.Border.Color = Color.Black;
                cell.VertAlign = VertAlign.Center;
                
                // Padding usunięty, bo powodował błędy na Dockerze
                // Domyślny padding jest OK
            }
            
            // Nagłówki kolumn w Panelu 2
            var headerObj = report.FindObject("Panel2Header") as FastReport.TextObject;
            if (headerObj != null)
            {
                headerObj.Text = string.Join(" | ", GetColumnNames(data));
                headerObj.Font = new Font("DejaVu Sans", 9, FontStyle.Bold);
                headerObj.TextFill = new SolidFill(Color.Black);
            }
            var p2b = report.FindObject("Panel2Body") as FastReport.TextObject;
            if(p2b != null) p2b.Text = "";
        }

        // --- Helpers ---

        private DataTable JsonToDataTable(List<Dictionary<string, object>> list)
        {
            DataTable table = new DataTable("Dane");
            if (list == null || list.Count == 0) return table;

            foreach (var key in list[0].Keys) table.Columns.Add(key, typeof(string));

            foreach (var item in list)
            {
                var row = table.NewRow();
                foreach (var key in item.Keys) row[key] = item[key]?.ToString() ?? "";
                table.Rows.Add(row);
            }
            return table;
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

        private string[] GetColumnNames(DataTable table)
        {
            string[] names = new string[table.Columns.Count];
            for(int i=0; i<table.Columns.Count; i++) names[i] = table.Columns[i].ColumnName;
            return names;
        }
    }
}
