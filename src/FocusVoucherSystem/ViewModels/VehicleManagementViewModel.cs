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
    private ObservableCollection<Vehicle> _vehicles = new();

    [ObservableProperty]
    private Vehicle _currentVehicle = new();

    [ObservableProperty]
    private Vehicle? _selectedVehicle;

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

    private List<Vehicle> _allVehicles = new();

    public VehicleManagementViewModel(DataService dataService) : base(dataService)
    {
        InitializeNewVehicle();
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
            _allVehicles = vehicles.OrderBy(v => v.VehicleNumber).ToList();

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
        foreach (var vehicle in filtered)
        {
            Vehicles.Add(vehicle);
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

        IsEditMode = false;
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
                _allVehicles.Add(savedVehicle);
                StatusMessage = $"Vehicle '{savedVehicle.VehicleNumber}' added successfully";
            }
            else
            {
                // Update existing vehicle
                savedVehicle = await _dataService.Vehicles.UpdateAsync(CurrentVehicle);
                
                // Replace in collection
                var index = _allVehicles.FindIndex(v => v.VehicleId == savedVehicle.VehicleId);
                if (index >= 0)
                {
                    _allVehicles[index] = savedVehicle;
                }
                
                StatusMessage = $"Vehicle '{savedVehicle.VehicleNumber}' updated successfully";
            }

            RefreshVehicleList();
            IsEditMode = false;
            InitializeNewVehicle();

        }, "Saving vehicle...");
    }

    /// <summary>
    /// Cancels the current edit operation
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
        InitializeNewVehicle();
        StatusMessage = "Edit cancelled";
    }

    /// <summary>
    /// Deletes a vehicle
    /// </summary>
    [RelayCommand]
    private async Task DeleteVehicle(Vehicle? vehicle)
    {
        if (vehicle == null) return;

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
                _allVehicles.Remove(vehicle);
                RefreshVehicleList();
                StatusMessage = $"Vehicle '{vehicle.VehicleNumber}' deleted successfully";
                
                if (SelectedVehicle == vehicle)
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
    partial void OnSelectedVehicleChanged(Vehicle? value)
    {
        if (value != null)
        {
            StatusMessage = $"Selected: {value.DisplayName}";
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