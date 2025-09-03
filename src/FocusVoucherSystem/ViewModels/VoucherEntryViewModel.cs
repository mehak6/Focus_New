using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// ViewModel for the voucher entry screen
/// </summary>
public partial class VoucherEntryViewModel : BaseViewModel, INavigationAware
{
    [ObservableProperty]
    private ObservableCollection<Voucher> _vouchers = new();

    [ObservableProperty]
    private ObservableCollection<Vehicle> _vehicles = new();

    [ObservableProperty]
    private Voucher _currentVoucher = new();

    [ObservableProperty]
    private Voucher? _selectedVoucher;

    [ObservableProperty]
    private Vehicle? _selectedVehicle;

    [ObservableProperty]
    private Company? _currentCompany;

    [ObservableProperty]
    private string _statusMessage = "Ready to enter vouchers";

    [ObservableProperty]
    private bool _isVoucherNumberReadOnly = true;

    [ObservableProperty]
    private int _totalVouchers;

    public VoucherEntryViewModel(DataService dataService) : base(dataService)
    {
        InitializeNewVoucher();
    }

    /// <summary>
    /// Loads vouchers and vehicles for the current company
    /// </summary>
    public async Task LoadDataAsync()
    {
        if (CurrentCompany == null) return;

        await ExecuteAsync(async () =>
        {
            // Load vehicles
            var vehicles = await _dataService.Vehicles.GetActiveByCompanyIdAsync(CurrentCompany.CompanyId);
            Vehicles.Clear();
            foreach (var vehicle in vehicles)
            {
                Vehicles.Add(vehicle);
            }

            // Load vouchers
            var vouchers = await _dataService.Vouchers.GetByCompanyIdAsync(CurrentCompany.CompanyId);
            Vouchers.Clear();
            foreach (var voucher in vouchers.OrderByDescending(v => v.Date).ThenByDescending(v => v.VoucherNumber))
            {
                Vouchers.Add(voucher);
            }

            TotalVouchers = Vouchers.Count;
            StatusMessage = $"Loaded {TotalVouchers} vouchers and {Vehicles.Count} vehicles";

        }, "Loading voucher data...");
    }

    /// <summary>
    /// Initializes a new voucher with default values
    /// </summary>
    private void InitializeNewVoucher()
    {
        CurrentVoucher = new Voucher
        {
            Date = DateTime.Today,
            DrCr = "D",
            Amount = 0m,
            CompanyId = CurrentCompany?.CompanyId ?? 1
        };

        if (CurrentCompany != null)
        {
            CurrentVoucher.VoucherNumber = CurrentCompany.GetNextVoucherNumber();
        }

        SelectedVehicle = null;
        StatusMessage = "New voucher ready";
    }

    /// <summary>
    /// Validates the current voucher data
    /// </summary>
    private bool ValidateCurrentVoucher()
    {
        var errors = CurrentVoucher.Validate();

        if (SelectedVehicle == null)
            errors.Add("Please select a vehicle");

        if (errors.Any())
        {
            StatusMessage = $"Validation errors: {string.Join(", ", errors)}";
            return false;
        }

        return true;
    }

    #region Commands

    /// <summary>
    /// Creates a new voucher (F2)
    /// </summary>
    [RelayCommand]
    private void NewVoucher()
    {
        InitializeNewVoucher();
    }

    /// <summary>
    /// Saves the current voucher (F5)
    /// </summary>
    [RelayCommand]
    private async Task SaveVoucher()
    {
        if (!ValidateCurrentVoucher())
            return;

        await ExecuteAsync(async () =>
        {
            // Set the vehicle ID
            CurrentVoucher.VehicleId = SelectedVehicle!.VehicleId;
            CurrentVoucher.CompanyId = CurrentCompany!.CompanyId;

            Voucher savedVoucher;

            if (CurrentVoucher.VoucherId == 0)
            {
                // New voucher
                savedVoucher = await _dataService.Vouchers.AddAsync(CurrentVoucher);
                
                // Update company's last voucher number
                await _dataService.Companies.UpdateLastVoucherNumberAsync(
                    CurrentCompany.CompanyId, 
                    CurrentVoucher.VoucherNumber);

                Vouchers.Insert(0, savedVoucher);
                TotalVouchers++;
                StatusMessage = $"Voucher {savedVoucher.VoucherNumber} saved successfully";
            }
            else
            {
                // Update existing voucher
                savedVoucher = await _dataService.Vouchers.UpdateAsync(CurrentVoucher);
                
                // Replace in collection
                var index = Vouchers.ToList().FindIndex(v => v.VoucherId == savedVoucher.VoucherId);
                if (index >= 0)
                {
                    Vouchers[index] = savedVoucher;
                }
                
                StatusMessage = $"Voucher {savedVoucher.VoucherNumber} updated successfully";
            }

            // Load fresh vehicle data for the saved voucher
            savedVoucher.Vehicle = await _dataService.Vehicles.GetByIdAsync(savedVoucher.VehicleId);

            InitializeNewVoucher();

        }, "Saving voucher...");
    }

    /// <summary>
    /// Deletes the selected voucher (F8)
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteVoucher))]
    private async Task DeleteVoucher()
    {
        if (SelectedVoucher == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete Voucher {SelectedVoucher.VoucherNumber}?\n\n" +
            $"Vehicle: {SelectedVoucher.Vehicle?.VehicleNumber}\n" +
            $"Amount: {SelectedVoucher.FormattedAmount}\n" +
            $"Narration: {SelectedVoucher.Narration}",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        await ExecuteAsync(async () =>
        {
            var success = await _dataService.Vouchers.DeleteAsync(SelectedVoucher.VoucherId);
            
            if (success)
            {
                Vouchers.Remove(SelectedVoucher);
                TotalVouchers--;
                StatusMessage = $"Voucher {SelectedVoucher.VoucherNumber} deleted successfully";
                SelectedVoucher = null;
            }
            else
            {
                StatusMessage = "Failed to delete voucher";
            }

        }, "Deleting voucher...");
    }

    private bool CanDeleteVoucher() => SelectedVoucher != null;

    /// <summary>
    /// Refreshes the voucher data
    /// </summary>
    [RelayCommand]
    private async Task RefreshData()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Searches vouchers by vehicle or narration
    /// </summary>
    [RelayCommand]
    private async Task SearchVouchers(string searchTerm)
    {
        if (CurrentCompany == null) return;

        await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                await LoadDataAsync();
                return;
            }

            var allVouchers = await _dataService.Vouchers.GetByCompanyIdAsync(CurrentCompany.CompanyId);
            var filtered = allVouchers.Where(v => 
                (v.Vehicle?.VehicleNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                (v.Narration?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true))
                .OrderByDescending(v => v.Date)
                .ThenByDescending(v => v.VoucherNumber);

            Vouchers.Clear();
            foreach (var voucher in filtered)
            {
                Vouchers.Add(voucher);
            }

            StatusMessage = $"Found {Vouchers.Count} vouchers matching '{searchTerm}'";

        }, "Searching vouchers...");
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handles selection of a voucher from the grid
    /// </summary>
    partial void OnSelectedVoucherChanged(Voucher? value)
    {
        if (value != null)
        {
            // Load the selected voucher into the form
            CurrentVoucher = value.Clone(value.VoucherNumber);
            CurrentVoucher.VoucherId = value.VoucherId; // Preserve ID for updates
            
            // Set the selected vehicle
            SelectedVehicle = Vehicles.FirstOrDefault(v => v.VehicleId == value.VehicleId);
            
            StatusMessage = $"Loaded voucher {value.VoucherNumber} for editing";
            IsVoucherNumberReadOnly = true;
        }
    }

    /// <summary>
    /// Handles vehicle selection changes
    /// </summary>
    partial void OnSelectedVehicleChanged(Vehicle? value)
    {
        if (value != null)
        {
            StatusMessage = $"Selected vehicle: {value.DisplayName}";
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
        // Cleanup if needed
        await Task.CompletedTask;
    }

    #endregion
}