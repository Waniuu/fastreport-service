using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using FastReport.Table; // Ważne dla tabel
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
        // ENDPOINTY (Wszystkie używają nowej funkcji GenerateBeautifulReport)
        // =========================================================
        
        [HttpPost("students-list")]
        public IActionResult GenerateUsers([FromBody] List<Dictionary<string, object>> data)
        {
            return GenerateBeautifulReport(data, "LISTA STUDENTÓW");
        }

        [HttpPost("exam-results")]
        public IActionResult GenerateExam([FromBody] List<Dictionary<string, object>> data)
        {
            return GenerateBeautifulReport(data, "WYNIKI EGZAMINU");
        }

        [HttpPost("questions-bank")]
        public IActionResult GenerateQuestions([FromBody] List<Dictionary<string, object>> data)
        {
            return GenerateBeautifulReport(data, "BANK PYTAŃ");
        }

        [HttpPost("tests-stats")]
        public IActionResult GenerateStats([FromBody] List<Dictionary<string, object>> data)
        {
            return GenerateBeautifulReport(data, "STATYSTYKA TESTÓW");
        }

        // =========================================================
        // LOGIKA GENEROWANIA "PIĘKNEGO" RAPORTU (Code-First)
        // =========================================================
        private IActionResult GenerateBeautifulReport(List<Dictionary<string, object>> jsonData, string reportTitle)
        {
            try
            {
                // 1. Konwersja JSON -> DataTable (dla łatwiejszej obsługi w FastReport)
                DataTable table = JsonToDataTable(jsonData);
                table.TableName = "Dane";

                // 2. Tworzymy pusty raport
                Report report = new Report();
                report.RegisterData(table, "Dane");
                report.GetDataSource("Dane").Enabled = true;

                // 3. STRONA
                ReportPage page = new ReportPage();
                page.Name = "Page1";
                // Ustaw marginesy
                page.LeftMargin = 10; 
                page.RightMargin = 10;
                page.TopMargin = 10;
                page.BottomMargin = 10;
                report.Pages.Add(page);

                // --------------------------------------------------------
                // A. NAGŁÓWEK RAPORTU (Różowy pasek)
                // --------------------------------------------------------
                ReportTitleBand titleBand = new ReportTitleBand();
                titleBand.Name = "ReportTitle";
                titleBand.Height = Units.Centimeters * 3.0f;
                page.ReportTitle = titleBand;

                // Różowe tło
                ShapeObject headerBg = new ShapeObject();
                headerBg.Parent = titleBand;
                headerBg.Bounds = new RectangleF(0, 0, page.Width, Units.Centimeters * 2.0f);
                headerBg.Fill = new SolidFill(Color.FromArgb(255, 51, 102)); // Twój #ff3366
                headerBg.ShapeKind = ShapeKind.Rectangle;
                headerBg.Border.Lines = BorderLines.None;

                // Główny tytuł (System...)
                TextObject sysTitle = new TextObject();
                sysTitle.Parent = titleBand;
                sysTitle.Bounds = new RectangleF(Units.Centimeters * 0.5f, Units.Centimeters * 0.2f, page.Width, Units.Centimeters * 1.0f);
                sysTitle.Text = "System Generowania Testów";
                sysTitle.Font = new Font("Arial", 18, FontStyle.Bold);
                sysTitle.TextColor = Color.White;

                // Data
                TextObject dateTxt = new TextObject();
                dateTxt.Parent = titleBand;
                dateTxt.Bounds = new RectangleF(Units.Centimeters * 0.5f, Units.Centimeters * 1.2f, page.Width, Units.Centimeters * 0.5f);
                dateTxt.Text = "Wygenerowano: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                dateTxt.Font = new Font("Arial", 10, FontStyle.Regular);
                dateTxt.TextColor = Color.White;

                // Tytuł konkretnego raportu (np. LISTA STUDENTÓW)
                TextObject subTitle = new TextObject();
                subTitle.Parent = titleBand;
                subTitle.Bounds = new RectangleF(0, Units.Centimeters * 2.2f, page.Width, Units.Centimeters * 0.8f);
                subTitle.Text = reportTitle;
                subTitle.Font = new Font("Arial", 14, FontStyle.Bold);
                subTitle.TextColor = Color.Black;

                // --------------------------------------------------------
                // B. NAGŁÓWEK TABELI (Ciemny pasek z nazwami kolumn)
                // --------------------------------------------------------
                PageHeaderBand headerBand = new PageHeaderBand();
                headerBand.Height = Units.Centimeters * 0.8f;
                page.PageHeader = headerBand;

                // Obliczamy szerokość kolumn (równy podział)
                float colWidth = page.Width / (table.Columns.Count > 0 ? table.Columns.Count : 1);
                float currentX = 0;

                foreach (DataColumn col in table.Columns)
                {
                    TextObject cell = new TextObject();
                    cell.Parent = headerBand;
                    cell.Bounds = new RectangleF(currentX, 0, colWidth, headerBand.Height);
                    cell.Text = col.ColumnName;
                    cell.Font = new Font("Arial", 10, FontStyle.Bold);
                    cell.Fill = new SolidFill(Color.FromArgb(40, 40, 40)); // Ciemnoszary
                    cell.TextColor = Color.White;
                    cell.HorzAlign = HorzAlign.Center;
                    cell.VertAlign = VertAlign.Center;
                    cell.Border.Lines = BorderLines.All;
                    cell.Border.Color = Color.Gray;
                    
                    currentX += colWidth;
                }

                // --------------------------------------------------------
                // C. DANE (Tabela w kratkę)
                // --------------------------------------------------------
                DataBand dataBand = new DataBand();
                dataBand.Height = Units.Centimeters * 0.8f;
                dataBand.DataSource = report.GetDataSource("Dane");
                page.Bands.Add(dataBand);

                currentX = 0;
                foreach (DataColumn col in table.Columns)
                {
                    TextObject cell = new TextObject();
                    cell.Parent = dataBand;
                    cell.Bounds = new RectangleF(currentX, 0, colWidth, dataBand.Height);
                    // Bindowanie danych [Dane.NazwaKolumny]
                    cell.Text = "[Dane." + col.ColumnName + "]"; 
                    cell.Font = new Font("Arial", 10, FontStyle.Regular);
                    cell.VertAlign = VertAlign.Center;
                    
                    // KRATKA EXCELOWA
                    cell.Border.Lines = BorderLines.All;
                    cell.Border.Color = Color.LightGray;

                    // Opcjonalnie: wyrównanie liczb do prawej
                    if (IsNumeric(col.DataType))
                        cell.HorzAlign = HorzAlign.Right;
                    
                    currentX += colWidth;
                }

                // 4. Generowanie PDF
                report.Prepare();
                
                using (MemoryStream ms = new MemoryStream())
                {
                    PDFSimpleExport export = new PDFSimpleExport();
                    export.Export(report, ms);
                    return File(ms.ToArray(), "application/pdf", "raport.pdf");
                }
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Błąd generowania PDF: {ex.Message} \n {ex.StackTrace}");
            }
        }

        // --- Helper: Konwersja JSON List do DataTable ---
        private DataTable JsonToDataTable(List<Dictionary<string, object>> list)
        {
            DataTable dt = new DataTable();
            if (list == null || list.Count == 0) return dt;

            // Tworzenie kolumn na podstawie pierwszego wiersza
            foreach (var key in list[0].Keys)
            {
                dt.Columns.Add(key);
            }

            // Dodawanie wierszy
            foreach (var item in list)
            {
                DataRow row = dt.NewRow();
                foreach (var key in item.Keys)
                {
                    // Obsługa nulli i typów
                    var val = item[key];
                    row[key] = val?.ToString() ?? "";
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        private bool IsNumeric(System.Type type)
        {
            if (type == null) return false;
            switch (System.Type.GetTypeCode(type))
            {
                case System.TypeCode.Byte:
                case System.TypeCode.SByte:
                case System.TypeCode.UInt16:
                case System.TypeCode.UInt32:
                case System.TypeCode.UInt64:
                case System.TypeCode.Int16:
                case System.TypeCode.Int32:
                case System.TypeCode.Int64:
                case System.TypeCode.Decimal:
                case System.TypeCode.Double:
                case System.TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
