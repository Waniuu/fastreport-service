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
        // LOGIKA GENEROWANIA - STYL "EXCEL" + RÓŻOWY NAGŁÓWEK
        // =========================================================
        private IActionResult GenerateBeautifulReport(List<Dictionary<string, object>> jsonData, string reportTitle)
        {
            try
            {
                DataTable table = JsonToDataTable(jsonData);
                table.TableName = "Dane";

                Report report = new Report();
                report.RegisterData(table, "Dane");
                report.GetDataSource("Dane").Enabled = true;

                ReportPage page = new ReportPage();
                page.Name = "Page1";
                // Marginesy
                page.LeftMargin = 10; 
                page.RightMargin = 10;
                page.TopMargin = 10;
                report.Pages.Add(page);

                // --- 1. NAGŁÓWEK STRONY (Różowy pasek) ---
                ReportTitleBand titleBand = new ReportTitleBand();
                titleBand.Height = Units.Centimeters * 2.5f;
                page.ReportTitle = titleBand;

                // Tło nagłówka (jako duży tekst bez treści, ale z tłem)
                TextObject headerBg = new TextObject();
                headerBg.Parent = titleBand;
                headerBg.Bounds = new RectangleF(0, 0, page.Width, Units.Centimeters * 2.0f);
                headerBg.Fill = new SolidFill(Color.FromArgb(255, 51, 102)); // Różowy #ff3366
                headerBg.Text = ""; 

                // Tytuł Główny
                TextObject mainTitle = new TextObject();
                mainTitle.Parent = titleBand;
                mainTitle.Bounds = new RectangleF(10, 10, page.Width - 20, 30);
                mainTitle.Text = "System Generowania Testów";
                mainTitle.Font = new Font("Arial", 18, FontStyle.Bold);
                mainTitle.TextColor = Color.White;
                mainTitle.Fill = new SolidFill(Color.Transparent);

                // Data i nazwa raportu
                TextObject subTitle = new TextObject();
                subTitle.Parent = titleBand;
                subTitle.Bounds = new RectangleF(10, 45, page.Width - 20, 20);
                subTitle.Text = $"{reportTitle} | Data: {System.DateTime.Now:yyyy-MM-dd HH:mm}";
                subTitle.Font = new Font("Arial", 11, FontStyle.Regular);
                subTitle.TextColor = Color.White;
                subTitle.Fill = new SolidFill(Color.Transparent);

                // --- 2. NAGŁÓWKI TABELI (Ciemnoszare) ---
                PageHeaderBand headerBand = new PageHeaderBand();
                headerBand.Height = Units.Centimeters * 0.8f;
                page.PageHeader = headerBand;

                float colWidth = page.Width / (table.Columns.Count > 0 ? table.Columns.Count : 1);
                float currentX = 0;

                foreach (DataColumn col in table.Columns)
                {
                    TextObject cell = new TextObject();
                    cell.Parent = headerBand;
                    cell.Bounds = new RectangleF(currentX, 0, colWidth, headerBand.Height);
                    cell.Text = col.ColumnName.ToUpper();
                    cell.Font = new Font("Arial", 10, FontStyle.Bold);
                    cell.Fill = new SolidFill(Color.FromArgb(50, 50, 50)); // Ciemne tło
                    cell.TextColor = Color.White;
                    cell.HorzAlign = HorzAlign.Center;
                    cell.VertAlign = VertAlign.Center;
                    cell.Border.Lines = BorderLines.All; // Ramka
                    cell.Border.Color = Color.White;
                    
                    currentX += colWidth;
                }

                // --- 3. DANE (Białe z kratką) ---
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
                    cell.Text = "[Dane." + col.ColumnName + "]";
                    cell.Font = new Font("Arial", 10, FontStyle.Regular);
                    cell.VertAlign = VertAlign.Center;
                    cell.HorzAlign = HorzAlign.Center;
                    
                    // TO TWORZY EFEKT EXCELA (Kratka)
                    cell.Border.Lines = BorderLines.All;
                    cell.Border.Color = Color.Black; 
                    
                    currentX += colWidth;
                }

                // --- 4. Generowanie ---
                report.Prepare();
                
                using (MemoryStream ms = new MemoryStream())
                {
                    // Używamy PDFSimpleExport - jest szybszy i mniej awaryjny na Linuxie
                    PDFSimpleExport export = new PDFSimpleExport();
                    export.Export(report, ms);
                    return File(ms.ToArray(), "application/pdf", "raport.pdf");
                }
            }
            catch (System.Exception ex)
            {
                // Zwracamy błąd JSON, żeby frontend wiedział co się stało
                return StatusCode(500, new { error = "Błąd C#", details = ex.Message });
            }
        }

        private DataTable JsonToDataTable(List<Dictionary<string, object>> list)
        {
            DataTable dt = new DataTable();
            if (list == null || list.Count == 0) return dt;
            foreach (var key in list[0].Keys) dt.Columns.Add(key);
            foreach (var item in list)
            {
                DataRow row = dt.NewRow();
                foreach (var key in item.Keys) row[key] = item[key]?.ToString() ?? "";
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}
