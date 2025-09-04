using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// ViewModel for the vehicle management screen
/// </summary>
public partial class VehicleManagementViewModel : BaseViewModel, INavigationAware
{
    [ObservableProperty]
    private ObservableCollection<VehicleDisplayItem> _vehicles = new();

    [ObservableProperty]
    private Vehicle _currentVehicle = new();

    [ObservableProperty]
    private VehicleDisplayItem? _selectedVehicle;

    [ObservableProperty]
    private Company? _currentCompany;

    [ObservableProperty]
    private string _statusMessage = "Ready to manage vehicles";

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private int _totalVehicles;

    [ObservableProperty]
    private int _activeVehicles;

    private List<VehicleDisplayItem> _allVehicles = new();

    public VehicleManagementViewModel(DataService dataService) : base(dataService)
    {
        InitializeNewVehicle();
        // Always show the form like voucher entry
        IsEditMode = true;
    }

    /// <summary>
    /// Loads vehicles for the current company
    /// </summary>
    public async Task LoadDataAsync()
    {
        if (CurrentCompany == null) return;

        await ExecuteAsync(async () =>
        {
            // Load all vehicles for the company
            var vehicles = await _dataService.Vehicles.GetByCompanyIdAsync(CurrentCompany.CompanyId);
            var vehicleDisplayItems = new List<VehicleDisplayItem>();

            // Create display items with balance and transaction data
            foreach (var vehicle in vehicles.OrderBy(v => v.VehicleNumber))
            {
                var displayItem = new VehicleDisplayItem(vehicle);
                
                // Load balance and last transaction date
                var balance = await _dataService.Vehicles.GetVehicleBalanceAsync(vehicle.VehicleId);
                var lastTransactionDate = await _dataService.Vehicles.GetLastTransactionDateAsync(vehicle.VehicleId);
                
                displayItem.UpdateBalance(balance);
                displayItem.UpdateLastTransactionDate(lastTransactionDate);
                
                vehicleDisplayItems.Add(displayItem);
            }

            _allVehicles = vehicleDisplayItems;
            RefreshVehicleList();
            StatusMessage = $"Loaded {TotalVehicles} vehicles ({ActiveVehicles} active)";

        }, "Loading vehicles...");
    }

    /// <summary>
    /// Refreshes the vehicle list with current filters
    /// </summary>
    private void RefreshVehicleList()
    {
        var filtered = _allVehicles.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLowerInvariant();
            filtered = filtered.Where(v => 
                v.VehicleNumber.ToLowerInvariant().Contains(term) ||
                (v.Description?.ToLowerInvariant().Contains(term) == true));
        }

        Vehicles.Clear();
        foreach (var vehicleDisplay in filtered)
        {
            Vehicles.Add(vehicleDisplay);
        }

        TotalVehicles = _allVehicles.Count;
        ActiveVehicles = _allVehicles.Count(v => v.IsActive);
    }

    /// <summary>
    /// Initializes a new vehicle with default values
    /// </summary>
    private void InitializeNewVehicle()
    {
        CurrentVehicle = new Vehicle
        {
            CompanyId = CurrentCompany?.CompanyId ?? 1,
            IsActive = true,
            VehicleNumber = string.Empty,
            Description = string.Empty
        };

        IsEditMode = true; // Always keep form visible
        StatusMessage = "Ready to add new vehicle";
    }

    /// <summary>
    /// Validates the current vehicle data
    /// </summary>
    private async Task<bool> ValidateCurrentVehicleAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentVehicle.VehicleNumber))
        {
            StatusMessage = "Vehicle number is required";
            return false;
        }

        if (CurrentCompany == null)
        {
            StatusMessage = "No company selected";
            return false;
        }

        // Check uniqueness
        var isUnique = await _dataService.Vehicles.IsVehicleNumberUniqueAsync(
            CurrentCompany.CompanyId, 
            CurrentVehicle.VehicleNumber, 
            CurrentVehicle.VehicleId == 0 ? null : CurrentVehicle.VehicleId);

        if (!isUnique)
        {
            StatusMessage = $"Vehicle number '{CurrentVehicle.VehicleNumber}' already exists";
            return false;
        }

        return true;
    }

    #region Commands

    /// <summary>
    /// Adds a new vehicle
    /// </summary>
    [RelayCommand]
    private void AddVehicle()
    {
        InitializeNewVehicle();
        IsEditMode = true;
        StatusMessage = "Enter vehicle details";
    }

    /// <summary>
    /// Creates a new vehicle (clears the form)
    /// </summary>
    [RelayCommand]
    private void NewVehicle()
    {
        InitializeNewVehicle();
        StatusMessage = "Ready for new vehicle entry";
    }

    /// <summary>
    /// Edits the selected vehicle
    /// </summary>
    [RelayCommand]
    private void EditVehicle(Vehicle? vehicle)
    {
        if (vehicle == null) return;

        CurrentVehicle = new Vehicle
        {
            VehicleId = vehicle.VehicleId,
            CompanyId = vehicle.CompanyId,
            VehicleNumber = vehicle.VehicleNumber,
            Description = vehicle.Description,
            IsActive = vehicle.IsActive
        };

        IsEditMode = true;
        StatusMessage = $"Editing vehicle: {vehicle.VehicleNumber}";
    }

    /// <summary>
    /// Saves the current vehicle
    /// </summary>
    [RelayCommand]
    private async Task SaveVehicle()
    {
        if (!await ValidateCurrentVehicleAsync())
            return;

        await ExecuteAsync(async () =>
        {
            CurrentVehicle.CompanyId = CurrentCompany!.CompanyId;

            Vehicle savedVehicle;

            if (CurrentVehicle.VehicleId == 0)
            {
                // New vehicle
                savedVehicle = await _dataService.Vehicles.AddAsync(CurrentVehicle);
                
                // Create display item for new vehicle
                var newDisplayItem = new VehicleDisplayItem(savedVehicle);
                var balance = await _dataService.Vehicles.GetVehicleBalanceAsync(savedVehicle.VehicleId);
                var lastTransactionDate = await _dataService.Vehicles.GetLastTransactionDateAsync(savedVehicle.VehicleId);
                
                newDisplayItem.UpdateBalance(balance);
                newDisplayItem.UpdateLastTransactionDate(lastTransactionDate);
                
                _allVehicles.Add(newDisplayItem);
                StatusMessage = $"Vehicle '{savedVehicle.VehicleNumber}' added successfully";
            }
            else
            {
                // Update existing vehicle
                savedVehicle = await _dataService.Vehicles.UpdateAsync(CurrentVehicle);
                
                // Update the display item in collection
                var displayItem = _allVehicles.FirstOrDefault(v => v.VehicleId == savedVehicle.VehicleId);
                if (displayItem != null)
                {
                    displayItem.UpdateVehicleData(savedVehicle);
                    
                    // Refresh balance and transaction data
                    var balance = await _dataService.Vehicles.GetVehicleBalanceAsync(savedVehicle.VehicleId);
                    var lastTransactionDate = await _dataService.Vehicles.GetLastTransactionDateAsync(savedVehicle.VehicleId);
                    
                    displayItem.UpdateBalance(balance);
                    displayItem.UpdateLastTransactionDate(lastTransactionDate);
                }
                
                StatusMessage = $"Vehicle '{savedVehicle.VehicleNumber}' updated successfully";
            }

            RefreshVehicleList();
            InitializeNewVehicle(); // Clear form after save

        }, "Saving vehicle...");
    }

    /// <summary>
    /// Cancels the current edit operation
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        InitializeNewVehicle();
        StatusMessage = "Edit cancelled";
    }

    /// <summary>
    /// Deletes a vehicle
    /// </summary>
    [RelayCommand]
    private async Task DeleteVehicle()
    {
        // Use the current vehicle being edited or selected vehicle
        Vehicle? vehicle = null;
        VehicleDisplayItem? displayItem = null;
        
        if (CurrentVehicle.VehicleId > 0)
        {
            vehicle = CurrentVehicle;
            displayItem = _allVehicles.FirstOrDefault(v => v.VehicleId == CurrentVehicle.VehicleId);
        }
        else if (SelectedVehicle != null)
        {
            vehicle = SelectedVehicle.Vehicle;
            displayItem = SelectedVehicle;
        }
        
        if (vehicle == null || vehicle.VehicleId == 0)
        {
            StatusMessage = "No vehicle selected for deletion";
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete vehicle '{vehicle.VehicleNumber}'?\n\n" +
            $"Description: {vehicle.Description}\n" +
            $"Status: {(vehicle.IsActive ? "Active" : "Inactive")}\n\n" +
            "Warning: This will also affect any existing vouchers for this vehicle.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        await ExecuteAsync(async () =>
        {
            var success = await _dataService.Vehicles.DeleteAsync(vehicle.VehicleId);
            
            if (success)
            {
                if (displayItem != null)
                {
                    _allVehicles.Remove(displayItem);
                }
                RefreshVehicleList();
                StatusMessage = $"Vehicle '{vehicle.VehicleNumber}' deleted successfully";
                
                // Clear the form and selection after deletion
                InitializeNewVehicle();
                SelectedVehicle = null;
            }
            else
            {
                StatusMessage = "Failed to delete vehicle";
            }

        }, "Deleting vehicle...");
    }

    /// <summary>
    /// Refreshes the vehicle data
    /// </summary>
    [RelayCommand]
    private async Task RefreshData()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Clears the search filter
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchTerm = string.Empty;
        RefreshVehicleList();
        StatusMessage = "Search cleared";
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handles search term changes
    /// </summary>
    partial void OnSearchTermChanged(string value)
    {
        RefreshVehicleList();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            StatusMessage = $"Showing all {TotalVehicles} vehicles";
        }
        else
        {
            StatusMessage = $"Found {Vehicles.Count} vehicles matching '{value}'";
        }
    }

    /// <summary>
    /// Handles vehicle selection changes
    /// </summary>
    partial void OnSelectedVehicleChanged(VehicleDisplayItem? value)
    {
        if (value != null)
        {
            // Populate the form with selected vehicle data
            CurrentVehicle = value.CreateVehicleCopy();
            
            StatusMessage = $"Editing: {value.DisplayName}";
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