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
    [NotifyCanExecuteChangedFor(nameof(DeleteVoucherCommand))]
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

            // Load recent vouchers (limited for performance)
            var allVouchers = await _dataService.Vouchers.GetByCompanyIdAsync(CurrentCompany.CompanyId);
            
            // The repository already limits to 50, so just add them to the collection
            Vouchers.Clear();
            foreach (var voucher in allVouchers)
            {
                Vouchers.Add(voucher);
            }

            // Get total count for status display
            var totalCount = await _dataService.Vouchers.CountByCompanyAsync(CurrentCompany.CompanyId);
            TotalVouchers = totalCount;
            StatusMessage = $"Loaded {Vouchers.Count} recent vouchers (showing latest {Vouchers.Count} of {TotalVouchers} total) and {Vehicles.Count} vehicles";

        }, "Loading voucher data...");
        await SetNextVoucherNumberAsync();
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
            CompanyId = CurrentCompany?.CompanyId ?? 0,
            VehicleId = 0
            // VoucherNumber defaults to 0 and will be set by SetNextVoucherNumberAsync
        };

        SelectedVehicle = null;
    }

    private async Task SetNextVoucherNumberAsync()
    {
        if (CurrentCompany != null)
        {
            try
            {
                var next = await _dataService.Companies.GetNextVoucherNumberAsync(CurrentCompany.CompanyId);
                CurrentVoucher.VoucherNumber = next;
                StatusMessage = $"Ready for voucher #{next}";
            }
            catch (Exception)
            {
                CurrentVoucher.VoucherNumber = CurrentCompany.GetNextVoucherNumber();
                StatusMessage = $"Ready for voucher #{CurrentVoucher.VoucherNumber} (using cached value)";
            }
        }
        else
        {
            // Fallback: Try to get a company if somehow CurrentCompany is null
            try
            {
                var companies = await _dataService.Companies.GetActiveCompaniesAsync();
                var defaultCompany = companies.FirstOrDefault();
                if (defaultCompany != null)
                {
                    CurrentCompany = defaultCompany;
                    var next = await _dataService.Companies.GetNextVoucherNumberAsync(CurrentCompany.CompanyId);
                    CurrentVoucher.VoucherNumber = next;
                    StatusMessage = $"Ready for voucher #{next} (recovered default company)";
                    return;
                }
            }
            catch (Exception)
            {
                // Recovery failed, continue to error handling
            }
            
            CurrentVoucher.VoucherNumber = 0;
            StatusMessage = "ERROR: No company selected - voucher number set to 0";
        }
    }

    /// <summary>
    /// Validates the current voucher data
    /// </summary>
    private bool ValidateCurrentVoucher()
    {
        // Set CompanyId before validation
        if (CurrentCompany != null)
        {
            CurrentVoucher.CompanyId = CurrentCompany.CompanyId;
        }

        // Set VehicleId before validation
        if (SelectedVehicle != null)
        {
            CurrentVoucher.VehicleId = SelectedVehicle.VehicleId;
        }

        var errors = CurrentVoucher.Validate();

        if (SelectedVehicle == null)
            errors.Add("Please select a vehicle");
        
        if (CurrentCompany == null)
            errors.Add("Company must be selected");

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
        _ = SetNextVoucherNumberAsync();
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
            // IDs are already set in ValidateCurrentVoucher

            Voucher savedVoucher;

            if (CurrentVoucher.VoucherId == 0)
            {
                // New voucher
                savedVoucher = await _dataService.Vouchers.AddAsync(CurrentVoucher);
                
                // Update company's last voucher number
                if (CurrentCompany != null)
                {
                    await _dataService.Companies.UpdateLastVoucherNumberAsync(
                        CurrentCompany.CompanyId, 
                        CurrentVoucher.VoucherNumber);
                    
                    // Update the in-memory company object immediately
                    CurrentCompany.LastVoucherNumber = CurrentVoucher.VoucherNumber;
                }

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

            // Prepare for next voucher after successful save
            InitializeNewVoucher();
            await SetNextVoucherNumberAsync();

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

            var searchResults = await _dataService.Vouchers.SearchVouchersAsync(CurrentCompany.CompanyId, searchTerm, 50);
            
            Vouchers.Clear();
            foreach (var voucher in searchResults)
            {
                Vouchers.Add(voucher);
            }

            StatusMessage = $"Found {Vouchers.Count} vouchers matching '{searchTerm}' (showing up to 50 results)";

        }, "Searching vouchers...");
    }

    /// <summary>
    /// Searches for a specific voucher by number
    /// </summary>
    [RelayCommand]
    private async Task SearchVoucher(string voucherNumber)
    {
        if (CurrentCompany == null) return;

        await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(voucherNumber))
            {
                StatusMessage = "Please enter a voucher number to search";
                return;
            }

            if (!int.TryParse(voucherNumber.Trim(), out int voucherNum))
            {
                StatusMessage = "Please enter a valid voucher number";
                return;
            }

            // Search for the specific voucher
            var voucher = await _dataService.Vouchers.GetByVoucherNumberAsync(CurrentCompany.CompanyId, voucherNum);
            
            Vouchers.Clear();
            if (voucher != null)
            {
                // Load vehicle information for the voucher
                voucher.Vehicle = await _dataService.Vehicles.GetByIdAsync(voucher.VehicleId);
                Vouchers.Add(voucher);
                StatusMessage = $"Found voucher #{voucherNum}";
            }
            else
            {
                StatusMessage = $"Voucher #{voucherNum} not found";
            }

        }, "Searching for voucher...");
    }

    /// <summary>
    /// Resets the view to show recent vouchers
    /// </summary>
    [RelayCommand]
    private async Task ResetView()
    {
        await LoadDataAsync();
        StatusMessage = "View reset to show recent vouchers";
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

        // Ensure Delete button updates its enabled state
        DeleteVoucherCommand?.NotifyCanExecuteChanged();
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
        System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: Started with parameters: {(parameters?.ToString() ?? "null")}");

        if (parameters is Company company)
        {
            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: Company parameter found - {company.Name} (ID: {company.CompanyId})");

            CurrentCompany = company;
            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: CurrentCompany set");

            await LoadDataAsync();
            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: LoadDataAsync completed");

            // Initialize a new voucher with the company set
            InitializeNewVoucher();
            await SetNextVoucherNumberAsync();
            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: Voucher initialization completed");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: No Company parameter found");
        }

        System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: Completed");
    }

    public async Task OnNavigatedFromAsync()
    {
        // Cleanup if needed
        await Task.CompletedTask;
    }

    #endregion
}
