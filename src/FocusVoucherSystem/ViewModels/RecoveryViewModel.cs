using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// ViewModel for the Recovery tab - shows vehicles with no credit transactions
/// </summary>
public partial class RecoveryViewModel : BaseViewModel, INavigationAware
{
    private readonly ExportService _exportService;

    [ObservableProperty]
    private int _days = 30;

    [ObservableProperty]
    private ObservableCollection<RecoveryItem> _recoveryItems = new();

    [ObservableProperty]
    private string _statusMessage = "Enter number of days and click Generate to find vehicles without credit transactions";

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
            
            int vehiclesWithoutCredits = 0;
            
            foreach (var vehicle in allVehicles)
            {
                // Get all credit transactions for this vehicle
                var vehicleVouchers = await _dataService.Vouchers.GetByVehicleIdAsync(vehicle.VehicleId);
                var creditVouchers = vehicleVouchers.Where(v => v.DrCr == "C").ToList();
                
                // Find the last credit transaction
                var lastCreditVoucher = creditVouchers
                    .OrderByDescending(v => v.Date)
                    .ThenByDescending(v => v.VoucherId)
                    .FirstOrDefault();
                
                string creditStatus;
                bool hasCredits;
                int daysSinceLastCredit;
                bool shouldInclude = false;
                
                if (lastCreditVoucher == null)
                {
                    // No credit transactions ever
                    creditStatus = "No credits ever";
                    hasCredits = false;
                    daysSinceLastCredit = 9999;
                    shouldInclude = true;
                }
                else if (lastCreditVoucher.Date < cutoffDate)
                {
                    // Last credit was before cutoff date
                    daysSinceLastCredit = (int)(DateTime.Today - lastCreditVoucher.Date).TotalDays;
                    creditStatus = $"{daysSinceLastCredit} days since last credit";
                    hasCredits = true;
                    shouldInclude = true;
                }
                else
                {
                    // Had credit within specified period - don't include
                    daysSinceLastCredit = (int)(DateTime.Today - lastCreditVoucher.Date).TotalDays;
                    creditStatus = $"{daysSinceLastCredit} days since last credit";
                    hasCredits = true;
                    shouldInclude = false;
                }
                
                // Only include vehicles that meet the criteria
                if (shouldInclude)
                {
                    vehiclesWithoutCredits++;
                    
                    RecoveryItems.Add(new RecoveryItem
                    {
                        VehicleNumber = vehicle.VehicleNumber,
                        Description = vehicle.Description ?? "-",
                        CreditStatus = creditStatus,
                        HasCredits = hasCredits,
                        DaysSinceLastCredit = daysSinceLastCredit
                    });
                }
            }
            
            // Sort by days since last credit (descending - most urgent first)
            var sortedItems = RecoveryItems.OrderByDescending(x => x.DaysSinceLastCredit).ToList();
            RecoveryItems.Clear();
            foreach (var item in sortedItems)
            {
                RecoveryItems.Add(item);
            }
            
            TotalVehicles = vehiclesWithoutCredits;
            StatusMessage = vehiclesWithoutCredits > 0 
                ? $"Found {vehiclesWithoutCredits} vehicles with no credit transactions in the last {Days} days"
                : $"No vehicles found without credit transactions in the last {Days} days";
                
        }, "Generating recovery statement...");
    }

    /// <summary>
    /// Clears the recovery results
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        RecoveryItems.Clear();
        TotalVehicles = 0;
        StatusMessage = "Enter number of days and click Generate to find vehicles without credit transactions";
    }

    /// <summary>
    /// Exports the recovery results to CSV
    /// </summary>
    [RelayCommand]
    private async Task Export()
    {
        if (!RecoveryItems.Any())
        {
            StatusMessage = "No data to export. Please generate the recovery statement first.";
            return;
        }

        try
        {
            // Convert RecoveryItems to a format suitable for CSV export
            var exportData = RecoveryItems.Select(item => new
            {
                VehicleNumber = item.VehicleNumber,
                Description = item.Description,
                CreditStatus = item.CreditStatus,
                DaysSinceLastCredit = item.DaysSinceLastCredit == 9999 ? "Never" : item.DaysSinceLastCredit.ToString()
            }).ToList();

            // Create a temporary file name
            var fileName = $"Recovery_Statement_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            // Write CSV
            using (var writer = new StreamWriter(filePath))
            {
                // Header
                writer.WriteLine("Vehicle Number,Description,Credit Status,Days Since Last Credit");
                
                // Data
                foreach (var item in exportData)
                {
                    writer.WriteLine($"\"{item.VehicleNumber}\",\"{item.Description}\",\"{item.CreditStatus}\",\"{item.DaysSinceLastCredit}\"");
                }
            }

            StatusMessage = $"Recovery statement exported to: {fileName}";
            
            // Open the folder containing the file
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    #region INavigationAware Implementation

    public async Task OnNavigatedToAsync(object? parameters)
    {
        if (parameters is Company company)
        {
            _currentCompany = company;
            StatusMessage = $"Recovery Statement for {company.Name} - Enter days and click Generate";
        }
    }

    public async Task OnNavigatedFromAsync()
    {
        // Cleanup if needed
        await Task.CompletedTask;
    }

    #endregion
}