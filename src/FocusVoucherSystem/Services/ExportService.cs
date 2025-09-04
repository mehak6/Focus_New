using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusVoucherSystem.Services;

/// <summary>
/// Handles exporting reports to CSV format (lightweight version)
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
        // PDF export disabled to reduce application size
        throw new NotSupportedException("PDF export has been disabled to reduce application size. Use CSV export instead.");
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
        // PDF export disabled to reduce application size
        throw new NotSupportedException("PDF export has been disabled to reduce application size. Use CSV export instead.");
    }

    public async Task<string> ExportExcelAsync(string reportKey, IEnumerable<ViewModels.ReportRow> rows)
    {
        // Excel export disabled to reduce application size
        throw new NotSupportedException("Excel export has been disabled to reduce application size. Use CSV export instead.");
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