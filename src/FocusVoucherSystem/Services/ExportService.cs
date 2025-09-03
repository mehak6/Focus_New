using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Handles exporting reports to CSV and PDF
/// </summary>
public class ExportService
{
    private static string EnsureExportDirectory()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var exportDir = Path.Combine(baseDir, "Exports");
        Directory.CreateDirectory(exportDir);
        return exportDir;
    }

    public async Task<string> ExportDayBookCsvAsync(IEnumerable<ViewModels.ReportRow> rows)
    {
        var exportDir = EnsureExportDirectory();
        var fileName = $"DayBook_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var filePath = Path.Combine(exportDir, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("Date,VoucherNumber,Vehicle,Narration,Amount,DrCr,RunningBalance");
        decimal totalDr = 0m, totalCr = 0m;
        foreach (var r in rows)
        {
            var line = string.Join(',',
                r.Date.ToString("yyyy-MM-dd"),
                r.VoucherNumber,
                EscapeCsv(r.VehicleNumber),
                EscapeCsv(r.Narration),
                r.Amount.ToString("0.00"),
                r.DrCr,
                r.RunningBalance.ToString("0.00"));
            sb.AppendLine(line);
            if (r.DrCr == "D") totalDr += r.Amount; else totalCr += r.Amount;
        }

        // Totals
        sb.AppendLine();
        sb.AppendLine($",,,Total Dr,{totalDr:0.00},D,");
        sb.AppendLine($",,,Total Cr,{totalCr:0.00},C,");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public async Task<string> ExportDayBookPdfAsync(IEnumerable<ViewModels.ReportRow> rows, string companyName)
    {
        var exportDir = EnsureExportDirectory();
        var fileName = $"DayBook_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(exportDir, fileName);

        var list = rows.ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Column(col =>
                {
                    col.Item().Text(companyName).SemiBold().FontSize(16);
                    col.Item().Text($"Day Book - Generated {DateTime.Now:yyyy-MM-dd HH:mm}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);   // Date
                        columns.RelativeColumn(1);   // V.No
                        columns.RelativeColumn(2);   // Vehicle
                        columns.RelativeColumn(5);   // Narration
                        columns.RelativeColumn(2);   // Amount
                        columns.RelativeColumn(1);   // DrCr
                        columns.RelativeColumn(2);   // Balance
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Text("Date").SemiBold();
                        header.Cell().Text("V.No").SemiBold();
                        header.Cell().Text("Vehicle").SemiBold();
                        header.Cell().Text("Narration").SemiBold();
                        header.Cell().AlignRight().Text("Amount").SemiBold();
                        header.Cell().Text("Dr/Cr").SemiBold();
                        header.Cell().AlignRight().Text("Balance").SemiBold();
                    });

                    foreach (var r in list)
                    {
                        table.Cell().Text(r.Date.ToString("dd/MM/yyyy"));
                        table.Cell().Text(r.VoucherNumber.ToString());
                        table.Cell().Text(r.VehicleNumber);
                        table.Cell().Text(r.Narration);
                        table.Cell().AlignRight().Text(r.Amount.ToString("N2"));
                        table.Cell().Text(r.DrCr);
                        table.Cell().AlignRight().Text(r.RunningBalance.ToString("N2"));
                    }

                    // Totals footer
                    var totalDr = list.Where(x => x.DrCr == "D").Sum(x => x.Amount);
                    var totalCr = list.Where(x => x.DrCr == "C").Sum(x => x.Amount);

                    table.Footer(footer =>
                    {
                        // Total Dr row
                        footer.Cell().Text("");
                        footer.Cell().Text("");
                        footer.Cell().Text("");
                        footer.Cell().AlignRight().Text("Total Dr:").SemiBold();
                        footer.Cell().AlignRight().Text(totalDr.ToString("N2")).SemiBold();
                        footer.Cell().Text("D").SemiBold();
                        footer.Cell().Text("");

                        // Total Cr row
                        footer.Cell().Text("");
                        footer.Cell().Text("");
                        footer.Cell().Text("");
                        footer.Cell().AlignRight().Text("Total Cr:").SemiBold();
                        footer.Cell().AlignRight().Text(totalCr.ToString("N2")).SemiBold();
                        footer.Cell().Text("C").SemiBold();
                        footer.Cell().Text("");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(filePath);

        // QuestPDF generate is synchronous; wrap in completed task for uniform API
        await Task.CompletedTask;
        return filePath;
    }

    public async Task<string> ExportCsvAsync(string reportKey, IEnumerable<ViewModels.ReportRow> rows)
    {
        var exportDir = EnsureExportDirectory();
        var safeKey = MakeFileSafe(reportKey);
        var fileName = $"{safeKey}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var filePath = Path.Combine(exportDir, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("Date,VoucherNumber,Vehicle,Narration,Amount,DrCr,RunningBalance");
        decimal totalDr = 0m, totalCr = 0m;
        foreach (var r in rows)
        {
            var line = string.Join(',',
                r.Date.ToString("yyyy-MM-dd"),
                r.VoucherNumber,
                EscapeCsv(r.VehicleNumber),
                EscapeCsv(r.Narration),
                r.Amount.ToString("0.00"),
                r.DrCr,
                r.RunningBalance.ToString("0.00"));
            sb.AppendLine(line);
            if (r.DrCr == "D") totalDr += r.Amount; else totalCr += r.Amount;
        }

        // Totals
        sb.AppendLine();
        sb.AppendLine($",,,Total Dr,{totalDr:0.00},D,");
        sb.AppendLine($",,,Total Cr,{totalCr:0.00},C,");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public async Task<string> ExportPdfAsync(string reportKey, IEnumerable<ViewModels.ReportRow> rows, string companyName)
    {
        var exportDir = EnsureExportDirectory();
        var safeKey = MakeFileSafe(reportKey);
        var fileName = $"{safeKey}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(exportDir, fileName);

        var list = rows.ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Column(col =>
                {
                    col.Item().Text(companyName).SemiBold().FontSize(16);
                    col.Item().Text($"{reportKey} - Generated {DateTime.Now:yyyy-MM-dd HH:mm}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(5);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Date").SemiBold();
                        header.Cell().Text("V.No").SemiBold();
                        header.Cell().Text("Vehicle").SemiBold();
                        header.Cell().Text("Narration").SemiBold();
                        header.Cell().AlignRight().Text("Amount").SemiBold();
                        header.Cell().Text("Dr/Cr").SemiBold();
                        header.Cell().AlignRight().Text("Balance").SemiBold();
                    });

                    foreach (var r in list)
                    {
                        table.Cell().Text(r.Date.ToString("dd/MM/yyyy"));
                        table.Cell().Text(r.VoucherNumber.ToString());
                        table.Cell().Text(r.VehicleNumber);
                        table.Cell().Text(r.Narration);
                        table.Cell().AlignRight().Text(r.Amount.ToString("N2"));
                        table.Cell().Text(r.DrCr);
                        table.Cell().AlignRight().Text(r.RunningBalance.ToString("N2"));
                    }

                    // Totals footer
                    var totalDr = list.Where(x => x.DrCr == "D").Sum(x => x.Amount);
                    var totalCr = list.Where(x => x.DrCr == "C").Sum(x => x.Amount);

                    table.Footer(footer =>
                    {
                        // Total Dr row
                        footer.Cell().Text("");
                        footer.Cell().Text("");
                        footer.Cell().Text("");
                        footer.Cell().AlignRight().Text("Total Dr:").SemiBold();
                        footer.Cell().AlignRight().Text(totalDr.ToString("N2")).SemiBold();
                        footer.Cell().Text("D").SemiBold();
                        footer.Cell().Text("");

                        // Total Cr row
                        footer.Cell().Text("");
                        footer.Cell().Text("");
                        footer.Cell().Text("");
                        footer.Cell().AlignRight().Text("Total Cr:").SemiBold();
                        footer.Cell().AlignRight().Text(totalCr.ToString("N2")).SemiBold();
                        footer.Cell().Text("C").SemiBold();
                        footer.Cell().Text("");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(filePath);

        await Task.CompletedTask;
        return filePath;
    }

    public async Task<string> ExportExcelAsync(string reportKey, IEnumerable<ViewModels.ReportRow> rows)
    {
        var exportDir = EnsureExportDirectory();
        var safeKey = MakeFileSafe(reportKey);
        var fileName = $"{safeKey}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var filePath = Path.Combine(exportDir, fileName);

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("Report");

        // Header
        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "VoucherNumber";
        ws.Cell(1, 3).Value = "Vehicle";
        ws.Cell(1, 4).Value = "Narration";
        ws.Cell(1, 5).Value = "Amount";
        ws.Cell(1, 6).Value = "DrCr";
        ws.Cell(1, 7).Value = "RunningBalance";
        ws.Range(1,1,1,7).Style.Font.Bold = true;

        // Rows
        int r = 2;
        decimal totalDr = 0m, totalCr = 0m;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.Date;
            ws.Cell(r, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(r, 2).Value = row.VoucherNumber;
            ws.Cell(r, 3).Value = row.VehicleNumber;
            ws.Cell(r, 4).Value = row.Narration;
            ws.Cell(r, 5).Value = row.Amount;
            ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r, 6).Value = row.DrCr;
            ws.Cell(r, 7).Value = row.RunningBalance;
            ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0.00";
            if (row.DrCr == "D") totalDr += row.Amount; else totalCr += row.Amount;
            r++;
        }

        // Totals
        if (r > 2)
        {
            ws.Cell(r + 0, 4).Value = "Total Dr:";
            ws.Cell(r + 0, 5).Value = totalDr;
            ws.Cell(r + 0, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r + 0, 4).Style.Font.Bold = true;
            ws.Cell(r + 0, 5).Style.Font.Bold = true;

            ws.Cell(r + 1, 4).Value = "Total Cr:";
            ws.Cell(r + 1, 5).Value = totalCr;
            ws.Cell(r + 1, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r + 1, 4).Style.Font.Bold = true;
            ws.Cell(r + 1, 5).Style.Font.Bold = true;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
        await Task.CompletedTask;
        return filePath;
    }

    private static string MakeFileSafe(string input)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
            input = input.Replace(ch, '_');
        return input.Replace(' ', '_');
    }

    private static string EscapeCsv(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        if (input.Contains('"') || input.Contains(',') || input.Contains('\n'))
        {
            return '"' + input.Replace("\"", "\"\"") + '"';
        }
        return input;
    }
}
