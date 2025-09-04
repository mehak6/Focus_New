using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// ViewModel for the search screen with dynamic vehicle search and voucher management
/// </summary>
public partial class SearchViewModel : BaseViewModel, INavigationAware
{
    [ObservableProperty]
    private ObservableCollection<Voucher> _vouchers = new();

    [ObservableProperty]
    private ObservableCollection<VehicleDisplayItem> _vehicleSearchResults = new();

    [ObservableProperty]
    private VehicleDisplayItem? _selectedVehicle;

    [ObservableProperty]
    private VehicleDisplayItem? _selectedVehicleFromSearch;

    [ObservableProperty]
    private Voucher? _selectedVoucher;

    [ObservableProperty]
    private Voucher _currentVoucher = new();

    [ObservableProperty]
    private Company? _currentCompany;

    [ObservableProperty]
    private string _statusMessage = "Enter vehicle name to search for vouchers";

    [ObservableProperty]
    private string _vehicleSearchTerm = string.Empty;

    [ObservableProperty]
    private bool _isVehicleSearchOpen;

    [ObservableProperty]
    private bool _isVoucherEditMode;

    [ObservableProperty]
    private int _totalVouchers;

    private List<VehicleDisplayItem> _allVehicles = new();
    private List<Voucher> _allVouchers = new();

    public SearchViewModel(DataService dataService) : base(dataService)
    {
        InitializeNewVoucher();
    }

    /// <summary>
    /// Loads initial data for the search screen
    /// </summary>
    public async Task LoadDataAsync()
    {
        if (CurrentCompany == null) return;

        await ExecuteAsync(async () =>
        {
            // Load all vehicles with balance information for search
            var vehicles = await _dataService.Vehicles.GetByCompanyIdAsync(CurrentCompany.CompanyId);
            var vehicleDisplayItems = new List<VehicleDisplayItem>();

            foreach (var vehicle in vehicles.OrderBy(v => v.VehicleNumber))
            {
                var displayItem = new VehicleDisplayItem(vehicle);
                var balance = await _dataService.Vehicles.GetVehicleBalanceAsync(vehicle.VehicleId);
                var lastTransactionDate = await _dataService.Vehicles.GetLastTransactionDateAsync(vehicle.VehicleId);
                
                displayItem.UpdateBalance(balance);
                displayItem.UpdateLastTransactionDate(lastTransactionDate);
                vehicleDisplayItems.Add(displayItem);
            }

            _allVehicles = vehicleDisplayItems;
            StatusMessage = $"Loaded {_allVehicles.Count} vehicles. Search for a vehicle to view vouchers.";

        }, "Loading vehicles...");
    }

    /// <summary>
    /// Loads vouchers for the selected vehicle
    /// </summary>
    private async Task LoadVouchersForVehicleAsync(VehicleDisplayItem vehicle)
    {
        await ExecuteAsync(async () =>
        {
            var vouchers = await _dataService.Vouchers.GetByVehicleIdAsync(vehicle.VehicleId);
            _allVouchers = vouchers.OrderByDescending(v => v.Date).ThenByDescending(v => v.VoucherNumber).ToList();
            
            // Calculate running balances
            CalculateRunningBalances();
            
            // Always clear and refresh the voucher list (even if empty)
            Vouchers.Clear();
            foreach (var voucher in _allVouchers)
            {
                Vouchers.Add(voucher);
            }

            TotalVouchers = _allVouchers.Count;
            
            // Force UI update
            OnPropertyChanged(nameof(Vouchers));
            OnPropertyChanged(nameof(TotalVouchers));
            
            // Update status message for both empty and non-empty cases
            if (TotalVouchers == 0)
            {
                StatusMessage = $"No vouchers found for {vehicle.DisplayName}. Current balance: {vehicle.FormattedBalance}";
            }
            else
            {
                StatusMessage = $"Found {TotalVouchers} vouchers for {vehicle.DisplayName}. Current balance: {vehicle.FormattedBalance}";
            }

        }, "Loading vouchers...");
    }

    /// <summary>
    /// Calculates running balances for vouchers
    /// </summary>
    private void CalculateRunningBalances()
    {
        if (_allVouchers.Count == 0) return;

        // Get vouchers in chronological order for running balance calculation
        var chronologicalVouchers = _allVouchers.OrderBy(v => v.Date).ThenBy(v => v.VoucherNumber).ToList();
        
        decimal runningBalance = 0;
        foreach (var voucher in chronologicalVouchers)
        {
            runningBalance += voucher.DrCr == "D" ? voucher.Amount : -voucher.Amount;
            voucher.RunningBalance = runningBalance;
        }
    }

    /// <summary>
    /// Filters vehicles based on search term
    /// </summary>
    private void FilterVehicleSearchResults()
    {
        VehicleSearchResults.Clear();

        if (string.IsNullOrWhiteSpace(VehicleSearchTerm))
        {
            IsVehicleSearchOpen = false;
            return;
        }

        var term = VehicleSearchTerm.ToLowerInvariant();
        var filtered = _allVehicles.Where(v =>
            v.VehicleNumber.ToLowerInvariant().Contains(term) ||
            (v.Description?.ToLowerInvariant().Contains(term) == true)).Take(10);

        foreach (var vehicle in filtered)
        {
            VehicleSearchResults.Add(vehicle);
        }

        IsVehicleSearchOpen = VehicleSearchResults.Count > 0;
    }

    /// <summary>
    /// Initializes a new voucher for editing
    /// </summary>
    private void InitializeNewVoucher()
    {
        CurrentVoucher = new Voucher
        {
            CompanyId = CurrentCompany?.CompanyId ?? 1,
            Date = DateTime.Today,
            Amount = 0,
            DrCr = "D",
            Narration = string.Empty
        };

        IsVoucherEditMode = false;
    }

    /// <summary>
    /// Refreshes vehicle balance after voucher changes
    /// </summary>
    private async Task RefreshVehicleBalanceAsync(int vehicleId)
    {
        var vehicle = _allVehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
        if (vehicle != null)
        {
            var balance = await _dataService.Vehicles.GetVehicleBalanceAsync(vehicleId);
            var lastTransactionDate = await _dataService.Vehicles.GetLastTransactionDateAsync(vehicleId);
            
            vehicle.UpdateBalance(balance);
            vehicle.UpdateLastTransactionDate(lastTransactionDate);
        }
    }

    #region Commands

    /// <summary>
    /// Clears the vehicle search
    /// </summary>
    [RelayCommand]
    private void ClearVehicleSearch()
    {
        VehicleSearchTerm = string.Empty;
        SelectedVehicle = null;
        Vouchers.Clear();
        IsVoucherEditMode = false;
        StatusMessage = "Enter vehicle name to search for vouchers";
    }

    /// <summary>
    /// Refreshes all data
    /// </summary>
    [RelayCommand]
    private async Task RefreshData()
    {
        await LoadDataAsync();
        if (SelectedVehicle != null)
        {
            await LoadVouchersForVehicleAsync(SelectedVehicle);
        }
    }

    /// <summary>
    /// Edits a voucher
    /// </summary>
    [RelayCommand]
    private void EditVoucher(Voucher? voucher)
    {
        if (voucher == null) return;

        CurrentVoucher = new Voucher
        {
            VoucherId = voucher.VoucherId,
            CompanyId = voucher.CompanyId,
            VehicleId = voucher.VehicleId,
            VoucherNumber = voucher.VoucherNumber,
            Date = voucher.Date,
            Amount = voucher.Amount,
            DrCr = voucher.DrCr,
            Narration = voucher.Narration,
            Vehicle = voucher.Vehicle
        };

        IsVoucherEditMode = true;
        StatusMessage = $"Editing voucher: {voucher.VoucherNumber}";
    }

    /// <summary>
    /// Updates the current voucher
    /// </summary>
    [RelayCommand]
    private async Task UpdateVoucher()
    {
        if (CurrentVoucher.VoucherId == 0)
        {
            StatusMessage = "No voucher selected for update";
            return;
        }

        if (CurrentVoucher.Amount <= 0)
        {
            StatusMessage = "Amount must be greater than zero";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var updatedVoucher = await _dataService.Vouchers.UpdateAsync(CurrentVoucher);
            
            IsVoucherEditMode = false;
            InitializeNewVoucher();
            StatusMessage = $"Voucher '{updatedVoucher.VoucherNumber}' updated successfully";
            
            // Programmatically trigger refresh
            await RefreshData();

        }, "Updating voucher...");
    }

    /// <summary>
    /// Cancels voucher editing
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsVoucherEditMode = false;
        InitializeNewVoucher();
        StatusMessage = "Edit cancelled";
    }

    /// <summary>
    /// Deletes a voucher
    /// </summary>
    [RelayCommand]
    private async Task DeleteVoucher(Voucher? voucher)
    {
        if (voucher == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete voucher '{voucher.VoucherNumber}'?\n\n" +
            $"Date: {voucher.Date:dd/MM/yyyy}\n" +
            $"Amount: {voucher.Amount.ToString("C2")} ({voucher.DrCr})\n" +
            $"Vehicle: {voucher.Vehicle?.DisplayName}\n" +
            $"Narration: {voucher.Narration}\n\n" +
            "This action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        await ExecuteAsync(async () =>
        {
            var success = await _dataService.Vouchers.DeleteAsync(voucher.VoucherId);
            
            if (success)
            {
                _allVouchers.Remove(voucher);
                
                StatusMessage = $"Voucher '{voucher.VoucherNumber}' deleted successfully";
                
                // Cancel edit mode if this voucher was being edited
                if (IsVoucherEditMode && CurrentVoucher.VoucherId == voucher.VoucherId)
                {
                    IsVoucherEditMode = false;
                    InitializeNewVoucher();
                }
                
                // Programmatically trigger refresh
                await RefreshData();
            }
            else
            {
                StatusMessage = "Failed to delete voucher";
            }

        }, "Deleting voucher...");
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handles vehicle search term changes
    /// </summary>
    partial void OnVehicleSearchTermChanged(string value)
    {
        FilterVehicleSearchResults();
    }

    /// <summary>
    /// Handles selected vehicle from search changes
    /// </summary>
    partial void OnSelectedVehicleFromSearchChanged(VehicleDisplayItem? value)
    {
        // This method is now simplified - most logic moved to SelectVehicle in code-behind
        // Keep it for potential future use
    }

    /// <summary>
    /// Handles selected vehicle changes
    /// </summary>
    partial void OnSelectedVehicleChanged(VehicleDisplayItem? value)
    {
        if (value != null)
        {
            // Use dispatcher to ensure UI thread execution
            _ = LoadVouchersForVehicleAsync(value);
        }
        else
        {
            Vouchers.Clear();
            TotalVouchers = 0;
            StatusMessage = "Enter vehicle name to search for vouchers";
        }
    }

    /// <summary>
    /// Handles voucher selection changes
    /// </summary>
    partial void OnSelectedVoucherChanged(Voucher? value)
    {
        if (value != null && !IsVoucherEditMode)
        {
            StatusMessage = $"Selected voucher: {value.VoucherNumber} - {value.FormattedAmount}";
        }
    }

    #endregion

    #region INavigationAware Implementation

    public async Task OnNavigatedToAsync(object? parameters)
    {
        if (parameters is Company company)
        {
            CurrentCompany = company;
            await LoadDataAsync();
        }
    }

    public async Task OnNavigatedFromAsync()
    {
        // Save any pending changes or cleanup
        await Task.CompletedTask;
    }

    #endregion
}
