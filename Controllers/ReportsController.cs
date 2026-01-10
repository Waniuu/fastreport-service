using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using FastReport.Table; // Ważne do tabel
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
        // ENDPOINTY (Wszystkie kierują do generatora tabeli)
        // =========================================================
        
        [HttpPost("students-list")]
        public IActionResult GenerateUsers([FromBody] List<Dictionary<string, object>> data)
        {
            return GenerateExcelStyleReport(data, "LISTA STUDENTÓW");
        }

        [HttpPost("exam-results")]
        public IActionResult GenerateExam([FromBody] List<Dictionary<string, object>> data)
        {
            return GenerateExcelStyleReport(data, "WYNIKI EGZAMINU");
        }

        [HttpPost("questions-bank")]
        public IActionResult GenerateQuestions([FromBody] List<Dictionary<string, object>> data)
        {
            return GenerateExcelStyleReport(data, "BANK PYTAŃ");
        }

        [HttpPost("tests-stats")]
        public IActionResult GenerateStats([FromBody] List<Dictionary<string, object>> data)
        {
            return GenerateExcelStyleReport(data, "STATYSTYKA TESTÓW");
        }

        // =========================================================
        // GENERATOR TABELI W STYLU EXCEL (BEZ PLIKÓW .FRX)
        // =========================================================
        private IActionResult GenerateExcelStyleReport(List<Dictionary<string, object>> jsonData, string reportTitle)
        {
            try
            {
                // 1. Przygotuj dane
                DataTable table = JsonToDataTable(jsonData);
                table.TableName = "Dane";

                // 2. Utwórz raport
                Report report = new Report();
                
                // 3. Utwórz stronę
                ReportPage page = new ReportPage();
                page.Name = "Page1";
                page.TopMargin = 10;
                page.BottomMargin = 10;
                page.LeftMargin = 10;
                page.RightMargin = 10;
                report.Pages.Add(page);

                // --- A. GÓRNY PASEK (Różowy Nagłówek) ---
                ReportTitleBand titleBand = new ReportTitleBand();
                titleBand.Height = Units.Centimeters * 2.5f;
                page.ReportTitle = titleBand;

                // Tło nagłówka (Różowy prostokąt)
                TextObject headerBg = new TextObject();
                headerBg.Parent = titleBand;
                headerBg.Bounds = new RectangleF(0, 0, page.Width, Units.Centimeters * 2.0f);
                headerBg.Fill = new SolidFill(Color.FromArgb(255, 51, 102)); // Twój kolor #ff3366
                headerBg.Text = ""; 

                // Tytuł Systemu
                TextObject sysTitle = new TextObject();
                sysTitle.Parent = titleBand;
                sysTitle.Bounds = new RectangleF(10, 10, page.Width - 20, 30);
                sysTitle.Text = "SYSTEM GENEROWANIA TESTÓW";
                sysTitle.Font = new Font("Arial", 16, FontStyle.Bold); // Arial jest bezpieczny
                sysTitle.TextColor = Color.White;
                sysTitle.Fill = new SolidFill(Color.Transparent);

                // Nazwa Raportu i Data
                TextObject subTitle = new TextObject();
                subTitle.Parent = titleBand;
                subTitle.Bounds = new RectangleF(10, 45, page.Width - 20, 20);
                subTitle.Text = $"{reportTitle}  |  {System.DateTime.Now:yyyy-MM-dd HH:mm}";
                subTitle.Font = new Font("Arial", 10, FontStyle.Regular);
                subTitle.TextColor = Color.White;
                subTitle.Fill = new SolidFill(Color.Transparent);

                // --- B. NAGŁÓWKI TABELI (Ciemnoszare) ---
                PageHeaderBand headerBand = new PageHeaderBand();
                headerBand.Height = Units.Centimeters * 0.8f;
                page.PageHeader = headerBand;

                // Obliczanie szerokości kolumn
                int colCount = table.Columns.Count > 0 ? table.Columns.Count : 1;
                float colWidth = page.Width / colCount;
                float currentX = 0;

                foreach (DataColumn col in table.Columns)
                {
                    TextObject cell = new TextObject();
                    cell.Parent = headerBand;
                    cell.Bounds = new RectangleF(currentX, 0, colWidth, headerBand.Height);
                    cell.Text = col.ColumnName.ToUpper();
                    cell.Font = new Font("Arial", 9, FontStyle.Bold);
                    cell.Fill = new SolidFill(Color.FromArgb(50, 50, 50)); // Ciemne tło
                    cell.TextColor = Color.White;
                    cell.HorzAlign = HorzAlign.Center;
                    cell.VertAlign = VertAlign.Center;
                    
                    // RAMKA (Biała dla nagłówka)
                    cell.Border.Lines = BorderLines.All;
                    cell.Border.Color = Color.White;
                    
                    currentX += colWidth;
                }

                // --- C. DANE (Prawdziwa Tabela) ---
                DataBand dataBand = new DataBand();
                dataBand.Height = Units.Centimeters * 0.7f;
                page.Bands.Add(dataBand);
                
                // Ręczne wiązanie danych (działa lepiej niż RegisterData w trybie Code-First)
                dataBand.BeforePrint += (sender, e) => {
                    // FastReport sam iteruje po wierszach, jeśli podepniemy źródło, 
                    // ale tutaj robimy to manualnie w pętli przy tworzeniu obiektów
                };

                // Ponieważ FastReport w trybie czystego kodu wymaga sprytnego podejścia do DataBand:
                // Zamiast bawić się w DataSource, użyjemy TableObject - to wygląda najlepiej (jak Excel)
                
                // UWAGA: Resetujemy stronę i używamy obiektu TABELA
                page.Bands.Clear(); // Usuwamy bandy, robimy TableObject
                page.ReportTitle = titleBand; // Przywracamy nagłówek

                DataBand masterData = new DataBand();
                masterData.Height = Units.Centimeters * 1.0f; // Wysokość dynamiczna
                page.Bands.Add(masterData);

                // Obiekt Tabela
                TableObject tableObj = new TableObject();
                tableObj.Parent = masterData;
                tableObj.Bounds = new RectangleF(0, 0, page.Width, Units.Centimeters * 2.0f);
                tableObj.RowCount = table.Rows.Count + 1; // Nagłówek + Dane
                tableObj.ColumnCount = colCount;
                
                // 1. Wiersz Nagłówka w Tabeli
                for (int c = 0; c < colCount; c++)
                {
                    tableObj[c, 0].Text = table.Columns[c].ColumnName;
                    tableObj[c, 0].Font = new Font("Arial", 9, FontStyle.Bold);
                    tableObj[c, 0].Fill = new SolidFill(Color.FromArgb(220, 220, 220)); // Jasnoszary
                    tableObj[c, 0].Border.Lines = BorderLines.All;
                    tableObj[c, 0].HorzAlign = HorzAlign.Center;
                    tableObj[c, 0].VertAlign = VertAlign.Center;
                }

                // 2. Wiersze Danych
                for (int r = 0; r < table.Rows.Count; r++)
                {
                    for (int c = 0; c < colCount; c++)
                    {
                        var row = table.Rows[r];
                        // +1 bo wiersz 0 to nagłówek
                        tableObj[c, r + 1].Text = row[c].ToString();
                        tableObj[c, r + 1].Font = new Font("Arial", 9, FontStyle.Regular);
                        tableObj[c, r + 1].Border.Lines = BorderLines.All; // KRATKA
                        tableObj[c, r + 1].Border.Color = Color.Black;
                        tableObj[c, r + 1].Padding = new Padding(2, 2, 2, 2);
                        
                        // Zmienny kolor wierszy (Zebra)
                        if (r % 2 == 1) 
                            tableObj[c, r + 1].Fill = new SolidFill(Color.FromArgb(245, 245, 245));
                    }
                }
                
                // Autosize
                for (int c = 0; c < colCount; c++)
                {
                    // Ustaw szerokość równomiernie
                    tableObj.Columns[c].Width = page.Width / colCount;
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
                return StatusCode(500, new { error = "Błąd C#", details = ex.Message });
            }
        }

        // Helper do konwersji JSON -> DataTable
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
