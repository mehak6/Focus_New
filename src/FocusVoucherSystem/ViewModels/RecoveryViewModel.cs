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

    [ObservableProperty]
    private int _days = 30;

    [ObservableProperty]
    private ObservableCollection<RecoveryItem> _recoveryItems = new();

    [ObservableProperty]
    private string _statusMessage = "Enter number of days and click Generate to find vehicles with positive balance and no transactions in that period";

    [ObservableProperty]
    private int _totalVehicles;

    private Company? _currentCompany;

    public RecoveryViewModel(DataService dataService) : base(dataService)
    {
        _exportService = new ExportService();
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
                    // Find the last transaction to show when it was
                    var lastVoucher = vehicleVouchers
                        .OrderByDescending(v => v.Date)
                        .ThenByDescending(v => v.VoucherId)
                        .FirstOrDefault();
                    
                    // Get current balance for this vehicle
                    var currentBalance = await _dataService.Vehicles.GetVehicleBalanceAsync(vehicle.VehicleId);
                    
                    // Skip vehicles with zero or negative balance
                    if (currentBalance <= 0)
                        continue;
                    
                    string transactionStatus;
                    int daysSinceLastTransaction;
                    decimal lastAmount = 0;
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
                        string transactionType = lastVoucher.DrCr == "C" ? "credit" : "debit";
                        transactionStatus = $"{daysSinceLastTransaction} days since last {transactionType}";
                        lastAmount = lastVoucher.Amount;
                        lastDate = lastVoucher.Date;
                    }
                    
                    // Extract vehicle group prefix (e.g., "UP-25" from "UP-25C-1234")
                    var groupPrefix = ExtractVehicleGroupPrefix(vehicle.VehicleNumber);
                    
                    inactiveVehicleItems.Add(new RecoveryItem
                    {
                        VehicleNumber = vehicle.VehicleNumber,
                        Description = vehicle.Description ?? "-",
                        LastAmount = lastAmount,
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
                // Add group header if there are multiple vehicles with same prefix
                if (group.Count() > 1)
                {
                    RecoveryItems.Add(new RecoveryItem
                    {
                        VehicleNumber = $"═══ {group.Key} VEHICLES ═══",
                        Description = string.Empty,
                        LastAmount = 0,
                        LastDate = null,
                        RemainingBalance = 0,
                        CreditStatus = $"{group.Count()} vehicles in this group",
                        HasCredits = false,
                        DaysSinceLastCredit = 0,
                        IsGroupHeader = true,
                        GroupPrefix = group.Key
                    });
                }
                
                // Add vehicles in this group, sorted by days since last transaction
                var sortedGroupVehicles = group.OrderByDescending(x => x.DaysSinceLastCredit).ToList();
                foreach (var vehicle in sortedGroupVehicles)
                {
                    RecoveryItems.Add(vehicle);
                }
            }
            
            TotalVehicles = inactiveVehicleItems.Count;
            StatusMessage = TotalVehicles > 0 
                ? $"Found {TotalVehicles} vehicles with positive balance and no transactions in the last {Days} days (grouped by vehicle type)"
                : $"No vehicles found with positive balance and no transactions in the last {Days} days";
                
        }, "Generating recovery statement...");
    }

    /// <summary>
    /// Extracts vehicle group prefix from vehicle number (e.g., "UP-25" from "UP-25C-1234")
    /// </summary>
    private string ExtractVehicleGroupPrefix(string vehicleNumber)
    {
        if (string.IsNullOrEmpty(vehicleNumber))
            return "OTHER";
            
        // Look for patterns like UP-25, WB-23, etc.
        // Extract state code and first number group
        var parts = vehicleNumber.Split('-');
        if (parts.Length >= 2)
        {
            var statePart = parts[0]; // e.g., "UP"
            var numberPart = parts[1]; // e.g., "25C" or "23B"
            
            // Extract just the number from the second part
            var numbers = new string(numberPart.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(numbers))
            {
                return $"{statePart}-{numbers}"; // e.g., "UP-25", "WB-23"
            }
        }
        
        // Fallback: use first 5 characters or the whole string if shorter
        return vehicleNumber.Length > 5 ? vehicleNumber.Substring(0, 5) : vehicleNumber;
    }

    /// <summary>
    /// Clears the recovery results
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        RecoveryItems.Clear();
        TotalVehicles = 0;
        StatusMessage = "Enter number of days and click Generate to find vehicles with positive balance and no transactions in that period";
    }

    /// <summary>
    /// Exports the recovery results to CSV
    /// </summary>
    [RelayCommand]
    private void Export()
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
            var fileName = $"Recovery_Statement_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(appDirectory, fileName);

            // Write CSV
            using (var writer = new StreamWriter(filePath))
            {
                // Header
                writer.WriteLine("Vehicle Type,Vehicle Number,Last Amount,Last Date,Remaining Balance,Transaction Status");
                
                // Data
                foreach (var item in RecoveryItems)
                {
                    if (item.IsGroupHeader)
                    {
                        // Group header row
                        writer.WriteLine($"\"{item.GroupPrefix}\",\"{item.VehicleNumber}\",\"\",\"\",\"\",\"{item.CreditStatus}\"");
                    }
                    else
                    {
                        // Regular vehicle row
                        var lastDateStr = item.LastDate?.ToString("dd/MM/yyyy") ?? "Never";
                        var lastAmountStr = item.LastAmount == 0 ? "0.00" : item.LastAmount.ToString("F2");
                        var balanceStr = item.RemainingBalance.ToString("F2");
                        
                        writer.WriteLine($"\"{item.GroupPrefix}\",\"{item.VehicleNumber}\",\"{lastAmountStr}\",\"{lastDateStr}\",\"{balanceStr}\",\"{item.CreditStatus}\"");
                    }
                }
            }

            StatusMessage = $"Recovery statement exported to: {fileName} in application folder";
            
            // Open the folder containing the file
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    #region INavigationAware Implementation

    public Task OnNavigatedToAsync(object? parameters)
    {
        if (parameters is Company company)
        {
            _currentCompany = company;
            StatusMessage = $"Recovery Statement for {company.Name} - Enter days to find vehicles with positive balance and no transactions in that period";
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