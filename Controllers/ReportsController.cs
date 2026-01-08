using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using FastReport.Table;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Drawing; // Do klasy Font i Color
using System.IO;
using System.Text.Json; // Do obsługi JSON
using System.Collections.Generic;

namespace FastReportService.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
        // =========================================================
        // UNIWERSALNA METODA GENEROWANIA DLA WSZYSTKICH RAPORTÓW
        // =========================================================
        
        [HttpPost("questions-stats")]
        public IActionResult GenerateStats([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "RAPORT STATYSTYK", "Dane wygenerowane z bazy");
        }

        [HttpPost("users")]
        public IActionResult GenerateUsers([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "LISTA UŻYTKOWNIKÓW", "Aktualny stan bazy danych");
        }

        [HttpPost("tests-grouped")]
        public IActionResult GenerateTests([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "HARMONOGRAM TESTÓW", "Lista utworzonych egzaminów");
        }

        [HttpPost("test-form")]
        public IActionResult GenerateCard([FromBody] List<Dictionary<string, object>> data)
        {
            return GeneratePdfFromData(data, "KARTA WYNIKÓW", "Szczegółowe wyniki studenta");
        }

        // =========================================================
        // LOGIKA: JSON -> DATATABLE -> PDF
        // =========================================================
        
   private IActionResult GeneratePdfFromData(List<Dictionary<string, object>> jsonData, string title, string subtitle)
        {
            try 
            {
                var report = new Report();
                report.Load("Reports/raport_testow.frx");
                
                // 1. Naprawiamy wygląd (fonty, kolory)
                FixReportVisuals(report);
                
                // 2. Ustawiamy nagłówki
                SetText(report, "Title", title);
                SetText(report, "Panel1Header", subtitle);
                SetText(report, "Panel1Body", $"Wygenerowano: {DateTime.Now:yyyy-MM-dd HH:mm}");

                // 3. Konwertujemy JSON na DataTable
                var table = JsonToDataTable(jsonData);

                // --- KLUCZOWA ZMIANA: CZYŚCIMY SZABLON ZAWSZE ---
                // Najpierw usuwamy stare śmieci ("Element listy...") z szablonu
                DataBand dataBand = report.FindObject("ListBand") as DataBand;
                if (dataBand != null)
                {
                    dataBand.Objects.Clear(); // Usuwa stary tekst z .frx
                }

                // 4. Budujemy dynamiczną tabelę TYLKO jak są dane
                if (table.Rows.Count > 0)
                {
                    BuildDynamicTable(report, table);
                }
                else
                {
                    // Jak nie ma danych, wpisujemy ładny komunikat zamiast tabeli
                    if (dataBand != null)
                    {
                        var noDataText = new FastReport.TextObject();
                        noDataText.Parent = dataBand;
                        noDataText.Bounds = new RectangleF(0, 0, 700, 30);
                        noDataText.Text = "BRAK DANYCH W BAZIE DLA WYBRANYCH KRYTERIÓW";
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

        // Helper: Zamiana listy słowników na DataTable
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
                foreach (var key in item.Keys)
                {
                    row[key] = item[key]?.ToString() ?? "";
                }
                table.Rows.Add(row);
            }

            return table;
        }

        // =========================================================
        // BUDOWANIE TABELI (BEZ PADDINGU)
        // =========================================================
        
        private void BuildDynamicTable(Report report, DataTable data)
        {
            report.RegisterData(data, "Dane");
            DataBand? dataBand = report.FindObject("ListBand") as DataBand;
            if (dataBand == null) return;

            dataBand.Objects.Clear();
            dataBand.DataSource = report.GetDataSource("Dane");

            TableObject table = new TableObject();
            table.Name = "DynamicTable";
            table.Parent = dataBand;
            table.Width = 700; 
            table.Height = 30; // Wysokość wiersza
            
            table.ColumnCount = data.Columns.Count;
            table.RowCount = 1; 
            
            float colWidth = 700f / data.Columns.Count;

            for (int i = 0; i < data.Columns.Count; i++)
            {
                table.Columns[i].Width = colWidth;
                TableCell cell = table[0, i];
                cell.Text = $"[Dane.{data.Columns[i].ColumnName}]";
                cell.Font = new Font("DejaVu Sans", 9, FontStyle.Regular);
                
                // Używamy SolidFill
                cell.TextFill = new SolidFill(Color.Black);
                
                cell.Border.Lines = BorderLines.All;
                cell.Border.Color = Color.Black;
                cell.VertAlign = VertAlign.Center;
                
                // USUWAMY LINIĘ Z PADDINGIEM, ABY UNIKNĄĆ BŁĘDU CS0234
                // cell.Padding = ... 
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
