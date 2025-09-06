using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Models;
using FocusVoucherSystem.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FocusVoucherSystem.ViewModels;

public partial class ReportsViewModel : BaseViewModel, INavigationAware
{
    private readonly ReportService _reportService;
    private readonly ExportService _exportService;
    private readonly PrintService _printService;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _reportTypes = new(new[] { "Day Book (Full Entries)", "Day Book (Consolidated)", "Vehicle Ledger", "Trial Balance" });

    [ObservableProperty]
    private string _selectedReportType = "Day Book (Full Entries)";

    [ObservableProperty]
    private DateTime _startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<Vehicle> _vehicles = new();

    [ObservableProperty]
    private Vehicle? _selectedVehicle;

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
    
    /// <summary>
    /// Handles report type selection changes - automatically sets from date for Vehicle Ledger
    /// </summary>
    partial void OnSelectedReportTypeChanged(string value)
    {
        if (value == "Vehicle Ledger")
        {
            // Set start date to January 1, 2001 for Vehicle Ledger reports
            StartDate = new DateTime(2001, 1, 1);
        }
        else
        {
            // Reset to default (first day of current month) for other reports
            StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }
    }

    private void UpdateNetTotals()
    {
        var net = TotalDebits - TotalCredits;
        NetDrCr = net >= 0 ? "D" : "C";
        NetAmount = Math.Abs(net);
    }

    [RelayCommand]
    private async Task GenerateReport()
    {
        if (_company == null) return;
        await ExecuteAsync(async () =>
        {
            ReportRows.Clear();

            if (SelectedReportType == "Day Book (Consolidated)")
            {
                decimal running = 0m;
                decimal debits = 0m, credits = 0m;

                var grouped = await _reportService.GetDayBookConsolidatedAsync(
                    _company.CompanyId, StartDate, EndDate, SelectedVehicle?.VehicleId);

                foreach (var e in grouped)
                {
                    var net = e.TotalDebits - e.TotalCredits;
                    var drCr = net >= 0 ? "D" : "C";
                    var amount = Math.Abs(net);
                    running += net;
                    debits += e.TotalDebits;
                    credits += e.TotalCredits;

                    ReportRows.Add(new ReportRow
                    {
                        Date = e.Date,
                        VoucherNumber = 0,
                        VehicleNumber = string.Empty,
                        Narration = "Summary",
                        Amount = amount,
                        DrCr = drCr,
                        RunningBalance = running
                    });
                }

                TotalDebits = debits;
                TotalCredits = credits;
            }
            else if (SelectedReportType == "Vehicle Ledger")
            {
                if (SelectedVehicle == null)
                {
                    StatusMessage = "Please select a vehicle";
                    return;
                }

                var ledger = await _reportService.GetVehicleLedgerAsync(SelectedVehicle.VehicleId, StartDate, EndDate);

                decimal running = ledger.OpeningBalance;
                decimal debits = 0m, credits = 0m;

                // Opening balance row (no voucher number)
                ReportRows.Add(new ReportRow
                {
                    Date = StartDate,
                    VoucherNumber = 0,
                    VehicleNumber = SelectedVehicle.VehicleNumber,
                    Narration = "Opening Balance",
                    Amount = Math.Abs(ledger.OpeningBalance),
                    DrCr = ledger.OpeningBalance >= 0 ? "D" : "C",
                    RunningBalance = running
                });

                foreach (var v in ledger.Entries)
                {
                    if (v.DrCr == "D") { running += v.Amount; debits += v.Amount; }
                    else { running -= v.Amount; credits += v.Amount; }

                    ReportRows.Add(new ReportRow
                    {
                        Date = v.Date,
                        VoucherNumber = v.VoucherNumber,
                        VehicleNumber = SelectedVehicle.VehicleNumber,
                        Narration = v.Narration ?? string.Empty,
                        Amount = v.Amount,
                        DrCr = v.DrCr,
                        RunningBalance = running
                    });
                }

                TotalDebits = debits;
                TotalCredits = credits;
            }
            else if (SelectedReportType == "Trial Balance")
            {
                if (_company == null) return;
                var trial = await _reportService.GetTrialBalanceAsync(_company.CompanyId, EndDate);
                decimal debits = 0m, credits = 0m;
                foreach (var t in trial)
                {
                    if (t.DrCr == "D") debits += t.Amount; else credits += t.Amount;
                    ReportRows.Add(new ReportRow
                    {
                        Date = EndDate,
                        VoucherNumber = 0,
                        VehicleNumber = t.Vehicle.VehicleNumber,
                        Narration = "Balance",
                        Amount = t.Amount,
                        DrCr = t.DrCr,
                        RunningBalance = 0m
                    });
                }
                TotalDebits = debits;
                TotalCredits = credits;
            }
            else
            {
                var result = await _reportService.GetDayBookAsync(
                    _company.CompanyId,
                    StartDate,
                    EndDate,
                    SelectedVehicle?.VehicleId);

                decimal running = 0m;
                decimal debits = 0m, credits = 0m;

                foreach (var v in result.OrderBy(v => v.Date).ThenBy(v => v.VoucherNumber))
                {
                    var amount = v.Amount;
                    if (v.DrCr == "D") { running += amount; debits += amount; }
                    else { running -= amount; credits += amount; }

                    ReportRows.Add(new ReportRow
                    {
                        Date = v.Date,
                        VoucherNumber = v.VoucherNumber,
                        VehicleNumber = v.Vehicle?.VehicleNumber ?? string.Empty,
                        Narration = v.Narration ?? string.Empty,
                        Amount = v.Amount,
                        DrCr = v.DrCr,
                        RunningBalance = running
                    });
                }

                TotalDebits = debits;
                TotalCredits = credits;
            }

            StatusMessage = $"Generated {ReportRows.Count} rows";
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

    public async Task OnNavigatedToAsync(object? parameters)
    {
        _company = parameters as Company;
        if (_company == null && parameters is not null)
        {
            _company = parameters as Company;
        }

        await ExecuteAsync(async () =>
        {
            Vehicles.Clear();
            if (_company != null)
            {
                var list = await _dataService.Vehicles.GetActiveByCompanyIdAsync(_company.CompanyId);
                foreach (var v in list) Vehicles.Add(v);
            }
            StatusMessage = "Reports ready";
        }, "Loading report data...");
    }

    public async Task OnNavigatedFromAsync()
    {
        await Task.CompletedTask;
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
}
