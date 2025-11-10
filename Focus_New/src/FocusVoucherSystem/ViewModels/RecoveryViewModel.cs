using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// ViewModel for the Recovery tab - shows vehicles with no transactions (credit or debit)
/// </summary>
public partial class RecoveryViewModel : BaseViewModel, INavigationAware
{
    private readonly ExportService _exportService;
    private readonly PrintService _printService;
    private readonly PdfExportService _pdfExportService;

    [ObservableProperty]
    private int _days = 30;

    [ObservableProperty]
    private decimal _minimumAmount = 0;

    [ObservableProperty]
    private ObservableCollection<RecoveryItem> _recoveryItems = new();

    [ObservableProperty]
    private string _statusMessage = "Enter number of days and minimum last credit amount, then click Generate to find vehicles";

    [ObservableProperty]
    private int _totalVehicles;

    private Company? _currentCompany;

    public RecoveryViewModel(DataService dataService) : base(dataService)
    {
        _exportService = new ExportService();
        _printService = new PrintService();
        _pdfExportService = new PdfExportService();
    }

    /// <summary>
    /// Generates the recovery statement
    /// </summary>
    [RelayCommand]
    private async Task GenerateRecovery()
    {
        if (_currentCompany == null)
        {
            StatusMessage = "No company selected";
            return;
        }

        if (Days <= 0)
        {
            StatusMessage = "Please enter a valid number of days (greater than 0)";
            return;
        }

        if (MinimumAmount < 0)
        {
            StatusMessage = "Please enter a valid minimum amount (0 or greater)";
            return;
        }

        await ExecuteAsync(async () =>
        {
            RecoveryItems.Clear();
            
            // Get all vehicles for the company
            var allVehicles = await _dataService.Vehicles.GetActiveByCompanyIdAsync(_currentCompany.CompanyId);
            var cutoffDate = DateTime.Today.AddDays(-Days);
            
            var inactiveVehicleItems = new List<RecoveryItem>();
            
            foreach (var vehicle in allVehicles)
            {
                // Get all transactions (both credit and debit) for this vehicle
                var vehicleVouchers = await _dataService.Vouchers.GetByVehicleIdAsync(vehicle.VehicleId);
                
                // Check if vehicle had ANY transaction within the specified period
                var hasRecentTransaction = vehicleVouchers.Any(v => v.Date >= cutoffDate);
                
                if (!hasRecentTransaction)
                {
                    // Find the last transaction (any type) to show when it was
                    var lastVoucher = vehicleVouchers
                        .OrderByDescending(v => v.Date)
                        .ThenByDescending(v => v.VoucherId)
                        .FirstOrDefault();
                    
                    // Find the last CREDIT transaction for amount display
                    var lastCreditVoucher = vehicleVouchers
                        .Where(v => v.DrCr == "C")
                        .OrderByDescending(v => v.Date)
                        .ThenByDescending(v => v.VoucherId)
                        .FirstOrDefault();
                    
                    // Get last credit amount first
                    decimal lastCreditAmount = 0;
                    if (lastCreditVoucher != null)
                    {
                        lastCreditAmount = lastCreditVoucher.Amount;
                    }

                    // Skip vehicles with last credit amount less than minimum amount
                    if (MinimumAmount > 0 && lastCreditAmount < MinimumAmount)
                        continue;

                    // Get current balance for this vehicle
                    var currentBalance = await _dataService.Vehicles.GetVehicleBalanceAsync(vehicle.VehicleId);

                    // Skip vehicles with zero or negative balance
                    if (currentBalance <= 0)
                        continue;

                    string transactionStatus;
                    int daysSinceLastTransaction;
                    DateTime? lastDate = null;

                    if (lastVoucher == null)
                    {
                        // No transactions ever
                        transactionStatus = "No transactions ever";
                        daysSinceLastTransaction = 9999;
                    }
                    else
                    {
                        // Last transaction was before cutoff date
                        daysSinceLastTransaction = (int)(DateTime.Today - lastVoucher.Date).TotalDays;
                        transactionStatus = daysSinceLastTransaction.ToString();
                        lastDate = lastVoucher.Date;
                    }
                    
                    // Extract vehicle group prefix (e.g., "UP-25" from "UP-25C-1234")
                    var groupPrefix = ExtractVehicleGroupPrefix(vehicle.VehicleNumber);
                    
                    inactiveVehicleItems.Add(new RecoveryItem
                    {
                        VehicleNumber = vehicle.VehicleNumber,
                        Description = vehicle.Narration ?? "-",
                        LastAmount = lastCreditAmount,
                        LastDate = lastDate,
                        RemainingBalance = currentBalance,
                        CreditStatus = transactionStatus,
                        HasCredits = lastVoucher != null,
                        DaysSinceLastCredit = daysSinceLastTransaction,
                        IsGroupHeader = false,
                        GroupPrefix = groupPrefix
                    });
                }
            }
            
            // Group vehicles by prefix and create grouped display
            var groupedVehicles = inactiveVehicleItems
                .GroupBy(v => v.GroupPrefix)
                .OrderBy(g => g.Key)
                .ToList();
            
            foreach (var group in groupedVehicles)
            {
                // Add group header only for groups with 3 or more vehicles (smart grouping)
                if (group.Count() >= 3)
                {
                    RecoveryItems.Add(new RecoveryItem
                    {
                        VehicleNumber = $"═══ {group.Key} VEHICLES ═══",
                        Description = string.Empty,
                        LastAmount = 0,
                        LastDate = null,
                        RemainingBalance = 0,
                        CreditStatus = string.Empty,
                        HasCredits = false,
                        DaysSinceLastCredit = 0,
                        IsGroupHeader = true,
                        GroupPrefix = group.Key
                    });
                }
                
                // Add vehicles in this group, sorted by vehicle number in ascending order
                var sortedGroupVehicles = group.OrderBy(x => x.VehicleNumber).ToList();
                foreach (var vehicle in sortedGroupVehicles)
                {
                    RecoveryItems.Add(vehicle);
                }
            }
            
            TotalVehicles = inactiveVehicleItems.Count;

            var amountFilter = MinimumAmount > 0 ? $" and last credit ≥ ₹{MinimumAmount:N2}" : "";
            StatusMessage = TotalVehicles > 0
                ? $"Found {TotalVehicles} vehicles with no transactions in last {Days} days{amountFilter} (grouped by vehicle type)"
                : $"No vehicles found with no transactions in last {Days} days{amountFilter}";
                
        }, "Generating recovery statement...");
    }

    /// <summary>
    /// Extracts vehicle group prefix from vehicle number (e.g., "UP-25-A" from "UP-25A-1234")
    /// </summary>
    private string ExtractVehicleGroupPrefix(string vehicleNumber)
    {
        if (string.IsNullOrEmpty(vehicleNumber))
            return "OTHER";
            
        // Look for patterns like UP-25A, UP-25B, WB-23C, etc.
        // Extract state code, number, and letter subcategory
        var parts = vehicleNumber.Split('-');
        if (parts.Length >= 2)
        {
            var statePart = parts[0]; // e.g., "UP"
            var numberPart = parts[1]; // e.g., "25C" or "23B"
            
            // Extract numbers and letters separately
            var numbers = new string(numberPart.Where(char.IsDigit).ToArray());
            var letters = new string(numberPart.Where(char.IsLetter).ToArray());
            
            if (!string.IsNullOrEmpty(numbers))
            {
                if (!string.IsNullOrEmpty(letters))
                {
                    // Include subcategory letter: e.g., "UP-25-A", "WB-23-B"
                    return $"{statePart}-{numbers}-{letters}";
                }
                else
                {
                    // No letter subcategory: e.g., "UP-25"
                    return $"{statePart}-{numbers}";
                }
            }
        }
        
        // Fallback: use first 6 characters or the whole string if shorter
        return vehicleNumber.Length > 6 ? vehicleNumber.Substring(0, 6) : vehicleNumber;
    }

    /// <summary>
    /// Clears the recovery results
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        RecoveryItems.Clear();
        TotalVehicles = 0;
        StatusMessage = "Enter number of days and minimum last credit amount, then click Generate to find vehicles";
    }

    /// <summary>
    /// Exports the recovery results to PDF
    /// </summary>
    [RelayCommand]
    private void ExportPdf()
    {
        if (!RecoveryItems.Any())
        {
            StatusMessage = "No data to export. Please generate the recovery statement first.";
            return;
        }

        try
        {
            // Get application directory
            var appDirectory = AppContext.BaseDirectory;

            // Create file name
            var fileName = $"Recovery_Statement_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(appDirectory, fileName);

            // Generate PDF
            var companyName = _currentCompany?.Name ?? "Focus Voucher System";
            _pdfExportService.GenerateRecoveryPdf(filePath, companyName, Days, RecoveryItems);

            StatusMessage = $"PDF exported to: {fileName} in application folder";

            // Open the folder containing the file
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch (Exception ex)
        {
            StatusMessage = $"PDF export failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Prints the recovery statement
    /// </summary>
    [RelayCommand]
    private Task PrintRecovery()
    {
        if (!RecoveryItems.Any())
        {
            StatusMessage = "No data to print. Please generate the recovery statement first.";
            return Task.CompletedTask;
        }

        try
        {
            var title = _currentCompany != null 
                ? $"Recovery Statement - {_currentCompany.Name} (Vehicles with no transactions in {Days} days)"
                : "Recovery Statement";
                
            _printService.PrintRecoveryDirectly(title, RecoveryItems);
            StatusMessage = "✅ Recovery statement sent to printer";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Print failed: {ex.Message}";
        }
        
        return Task.CompletedTask;
    }

    #region INavigationAware Implementation

    public Task OnNavigatedToAsync(object? parameters)
    {
        if (parameters is Company company)
        {
            _currentCompany = company;
            StatusMessage = $"Recovery Statement for {company.Name} - Enter days and minimum last credit amount to find vehicles";
        }
        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        // Cleanup if needed
        return Task.CompletedTask;
    }

    #endregion
}