using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Models;
using FocusVoucherSystem.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FocusVoucherSystem.Data.Repositories;

namespace FocusVoucherSystem.ViewModels;

public partial class ReportsViewModel : BaseViewModel, INavigationAware
{
    private readonly ReportService _reportService;
    private readonly ExportService _exportService;
    private readonly PrintService _printService;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _reportTypes = new(new[] {
        "Day Book (Full Entries)",
        "Day Book (Consolidated)",
        "Trial Balance",
        "Ledger (Full Entries)"
    });

    [ObservableProperty]
    private string _selectedReportType = "Day Book (Full Entries)";

    [ObservableProperty]
    private DateTime _startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;


    [ObservableProperty]
    private ObservableCollection<ReportRow> _reportRows = new();

    [ObservableProperty]
    private decimal _totalDebits;

    [ObservableProperty]
    private decimal _totalCredits;

    [ObservableProperty]
    private decimal _netAmount;

    [ObservableProperty]
    private string _netDrCr = "D";


    private Company? _company;

    public ReportsViewModel(DataService dataService, NavigationService navigationService)
        : base(dataService)
    {
        _reportService = new ReportService(dataService);
        _exportService = new ExportService();
        _printService = new PrintService();
    }

    partial void OnTotalDebitsChanged(decimal value) => UpdateNetTotals();
    partial void OnTotalCreditsChanged(decimal value) => UpdateNetTotals();
    


    private void UpdateNetTotals()
    {
        var net = TotalDebits - TotalCredits;
        NetDrCr = net >= 0 ? "D" : "C";
        NetAmount = Math.Abs(net);
    }



    [RelayCommand]
    private async Task GenerateReport()
    {
        if (_company == null) 
        {
            StatusMessage = "❌ No company selected";
            return;
        }
        
        await ExecuteAsync(async () =>
        {
            ReportRows.Clear();
            StatusMessage = $"🔄 Generating {SelectedReportType} from {StartDate:dd/MM/yyyy} to {EndDate:dd/MM/yyyy}...";

            if (SelectedReportType == "Day Book (Consolidated)")
            {
                decimal running = 0m;
                decimal debits = 0m, credits = 0m;

                var grouped = await _reportService.GetDayBookConsolidatedAsync(
                    _company.CompanyId, StartDate, EndDate);

                var tempRows = new List<ReportRow>();
                foreach (var e in grouped)
                {
                    var net = e.TotalDebits - e.TotalCredits;
                    var drCr = net >= 0 ? "D" : "C";
                    var amount = Math.Abs(net);
                    running += net;
                    debits += e.TotalDebits;
                    credits += e.TotalCredits;

                    tempRows.Add(new ReportRow
                    {
                        Date = e.Date,
                        VoucherNumber = 0,
                        VehicleNumber = string.Empty,
                        Narration = "Summary",
                        Amount = amount,
                        DrCr = drCr,
                        RunningBalance = running,
                        DebitBalance = 0,
                        CreditBalance = 0
                    });
                }

                // Update UI on main thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var row in tempRows)
                    {
                        ReportRows.Add(row);
                    }
                });

                TotalDebits = debits;
                TotalCredits = credits;
            }
            else if (SelectedReportType == "Trial Balance")
            {
                if (_company == null) return;
                var trial = await _reportService.GetTrialBalanceAsync(_company.CompanyId, EndDate);
                decimal debits = 0m, credits = 0m;
                var tempRows = new List<ReportRow>();

                foreach (var t in trial)
                {
                    if (t.DrCr == "D") debits += t.Amount; else credits += t.Amount;
                    tempRows.Add(new ReportRow
                    {
                        Date = EndDate,
                        VoucherNumber = 0,
                        VehicleNumber = t.Vehicle.VehicleNumber,
                        Narration = "Balance",
                        Amount = t.Amount,
                        DrCr = t.DrCr,
                        RunningBalance = 0m,
                        DebitBalance = 0,
                        CreditBalance = 0
                    });
                }

                // Update UI on main thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var row in tempRows)
                    {
                        ReportRows.Add(row);
                    }
                });

                TotalDebits = debits;
                TotalCredits = credits;
            }
            else
            {
                // Perform database query and processing in background thread
                var (processedRows, totalDebits, totalCredits, voucherCount) = await Task.Run(async () =>
                {
                    var result = await _reportService.GetDayBookAsync(
                        _company.CompanyId,
                        StartDate,
                        EndDate);

                    // Convert to list once to avoid multiple enumeration
                    var vouchers = result as List<Voucher> ?? result.ToList();
                    var count = vouchers.Count;

                    // Process vouchers in background thread with day-wise totals
                    decimal totalDebits = 0m, totalCredits = 0m;
                    decimal running = 0m;
                    var tempRows = new List<ReportRow>(count + count / 10); // Pre-allocate with estimated size
                    DateTime? lastDate = null;
                    decimal dayDebits = 0m, dayCredits = 0m;

                    // Vouchers are already sorted by SQL, no need to sort again
                    foreach (var v in vouchers)
                    {
                        // When date changes, add day total row and reset day counters
                        if (lastDate.HasValue && v.Date != lastDate.Value)
                        {
                            // Add day total row
                            var dayNet = dayDebits - dayCredits;
                            tempRows.Add(new ReportRow
                            {
                                Date = lastDate.Value,
                                VoucherNumber = 0,
                                VehicleNumber = "DAY TOTAL",
                                Narration = $"Total for {lastDate.Value:dd/MM/yyyy} - Dr: ₹{dayDebits:N2}, Cr: ₹{dayCredits:N2}",
                                Amount = Math.Abs(dayNet),
                                DrCr = dayNet >= 0 ? "D" : "C",
                                RunningBalance = running,
                                DebitBalance = dayDebits,
                                CreditBalance = dayCredits
                            });

                            // Add spacing row
                            tempRows.Add(new ReportRow
                            {
                                Date = DateTime.MinValue, // Special marker for spacing row
                                VoucherNumber = 0,
                                VehicleNumber = string.Empty,
                                Narration = "─────────────────────────────────────────────────────────────────────────────────────────────────────────",
                                Amount = 0,
                                DrCr = string.Empty,
                                RunningBalance = 0,
                                DebitBalance = 0,
                                CreditBalance = 0
                            });

                            // Reset day counters
                            dayDebits = 0m;
                            dayCredits = 0m;
                        }

                        var amount = v.Amount;
                        if (v.DrCr == "D")
                        {
                            running += amount;
                            totalDebits += amount;
                            dayDebits += amount;
                        }
                        else
                        {
                            running -= amount;
                            totalCredits += amount;
                            dayCredits += amount;
                        }

                        tempRows.Add(new ReportRow
                        {
                            Date = v.Date,
                            VoucherNumber = v.VoucherNumber,
                            VehicleNumber = v.Vehicle?.VehicleNumber ?? string.Empty,
                            Narration = v.Narration ?? string.Empty,
                            Amount = v.Amount,
                            DrCr = v.DrCr,
                            RunningBalance = running,
                            DebitBalance = dayDebits,
                            CreditBalance = dayCredits
                        });

                        lastDate = v.Date;
                    }

                    // Add final day total row if we processed any vouchers
                    if (lastDate.HasValue)
                    {
                        var finalDayNet = dayDebits - dayCredits;
                        tempRows.Add(new ReportRow
                        {
                            Date = lastDate.Value,
                            VoucherNumber = 0,
                            VehicleNumber = "DAY TOTAL",
                            Narration = $"Total for {lastDate.Value:dd/MM/yyyy} - Dr: ₹{dayDebits:N2}, Cr: ₹{dayCredits:N2}",
                            Amount = Math.Abs(finalDayNet),
                            DrCr = finalDayNet >= 0 ? "D" : "C",
                            RunningBalance = running,
                            DebitBalance = dayDebits,
                            CreditBalance = dayCredits
                        });
                    }

                    return (tempRows, totalDebits, totalCredits, count);
                });

                StatusMessage = $"📊 Retrieved {voucherCount} vouchers from database...";

                // Update UI on main thread in batches for better responsiveness
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ReportRows.Clear();
                    
                    // Add rows in batches for better UI performance
                    const int batchSize = 500;
                    for (int i = 0; i < processedRows.Count; i += batchSize)
                    {
                        var batch = processedRows.Skip(i).Take(batchSize);
                        foreach (var row in batch)
                        {
                            ReportRows.Add(row);
                        }
                    }
                });

                TotalDebits = totalDebits;
                TotalCredits = totalCredits;
            }

            StatusMessage = $"✅ Generated {ReportRows.Count} rows - Dr: ₹{TotalDebits:N2}, Cr: ₹{TotalCredits:N2}";
        }, "Generating report...");
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        if (ReportRows.Count == 0) { StatusMessage = "Please generate a report first"; return; }
        await ExecuteAsync(async () =>
        {
            try
            {
                var filePath = await _exportService.ExportCsvAsync(SelectedReportType, ReportRows);
                StatusMessage = $"✅ CSV exported successfully: {Path.GetFileName(filePath)}";
                
                // Open the export folder
                OpenExportFolder(filePath);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ CSV export failed: {ex.Message}";
            }
        }, "Exporting to CSV...");
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (ReportRows.Count == 0) { StatusMessage = "Please generate a report first"; return; }
        await ExecuteAsync(async () =>
        {
            try
            {
                var filePath = await _exportService.ExportPdfAsync(SelectedReportType, ReportRows, _company?.Name ?? "Company");
                StatusMessage = $"✅ PDF exported successfully: {Path.GetFileName(filePath)}";
                
                // Open the export folder
                OpenExportFolder(filePath);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ PDF export failed: {ex.Message}";
            }
        }, "Exporting to PDF...");
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        if (ReportRows.Count == 0) { StatusMessage = "Please generate a report first"; return; }
        await ExecuteAsync(async () =>
        {
            try
            {
                var filePath = await _exportService.ExportExcelAsync(SelectedReportType, ReportRows);
                StatusMessage = $"✅ Excel exported successfully: {Path.GetFileName(filePath)}";
                
                // Open the export folder
                OpenExportFolder(filePath);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Excel export failed: {ex.Message}";
            }
        }, "Exporting to Excel...");
    }

    [RelayCommand]
    private Task Print()
    {
        if (ReportRows.Count == 0) { StatusMessage = "Please generate a report first"; return Task.CompletedTask; }
        try
        {
            var title = _company != null ? $"{SelectedReportType} - {_company.Name} ({StartDate:dd/MM/yyyy} to {EndDate:dd/MM/yyyy})" : SelectedReportType;
            _printService.PreviewReport(title, ReportRows);
            StatusMessage = "✅ Print preview opened successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Print preview failed: {ex.Message}";
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task PrintReport()
    {
        if (ReportRows.Count == 0) { StatusMessage = "Please generate a report first"; return Task.CompletedTask; }
        try
        {
            var title = _company != null ? $"{SelectedReportType} - {_company.Name} ({StartDate:dd/MM/yyyy} to {EndDate:dd/MM/yyyy})" : SelectedReportType;
            _printService.PrintReportDirectly(title, ReportRows);
            StatusMessage = "✅ Report sent to printer";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Print failed: {ex.Message}";
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RefreshData()
    {
        if (_company != null)
        {
            await OnNavigatedToAsync(_company);
            StatusMessage = "✅ Data refreshed successfully";
        }
    }

    private static void OpenExportFolder(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (Directory.Exists(directory))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception)
        {
            // If selecting file fails, try opening folder
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                // Ignore if both methods fail
            }
        }
    }

    public Task OnNavigatedToAsync(object? parameters)
    {
        _company = parameters as Company;
        if (_company == null && parameters is not null)
        {
            _company = parameters as Company;
        }

        StatusMessage = "Reports ready";
        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }
}

public class ReportRow
{
    public DateTime Date { get; set; }
    public int VoucherNumber { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string DrCr { get; set; } = "D";
    public decimal RunningBalance { get; set; }
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
}
