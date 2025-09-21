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

    // Vehicle search UX state
    [ObservableProperty]
    private string _vehicleSearchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Vehicle> _filteredVehicles = new();

    [ObservableProperty]
    private bool _showVehicleSuggestions;

    [ObservableProperty]
    private Company? _currentCompany;

    [ObservableProperty]
    private string _statusMessage = "Ready to enter vouchers";

    [ObservableProperty]
    private bool _isVoucherNumberReadOnly = true;

    [ObservableProperty]
    private int _totalVouchers;

    private DateTime _lastSelectedDate = DateTime.Today;
    private string _lastSelectedDrCr = "D"; // Remember last Dr/Cr selection

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private decimal? _enteredAmount;

    public VoucherEntryViewModel(DataService dataService) : base(dataService)
    {
        InitializeNewVoucher();
    }

    // Request from VM to set focus on vehicle search textbox
    public event Action? FocusVehicleSearchRequested;

    // Request from VM to set focus on amount field
    public event Action? FocusAmountFieldRequested;

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

            // Initialize vehicle search list
            UpdateVehicleFilter();

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
            Date = _lastSelectedDate,
            DrCr = _lastSelectedDrCr, // Use remembered Dr/Cr selection
            Amount = 0m,
            CompanyId = CurrentCompany?.CompanyId ?? 0,
            VehicleId = 0
            // VoucherNumber defaults to 0 and will be set by SetNextVoucherNumberAsync
        };

        // Only update SelectedDate if it's different from the last selected date
        // This preserves user's date selection across navigation
        if (SelectedDate != _lastSelectedDate)
        {
            SelectedDate = _lastSelectedDate;
        }

        EnteredAmount = null; // Start with blank amount field
        SelectedVehicle = null;
        VehicleSearchText = string.Empty;
        UpdateVehicleFilter();
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
    /// Validates the current voucher data with enhanced error handling
    /// </summary>
    private Task<bool> ValidateCurrentVoucherAsync()
    {
        // Set basic required fields
        if (CurrentCompany != null)
            CurrentVoucher.CompanyId = CurrentCompany.CompanyId;
        
        if (SelectedVehicle != null)
            CurrentVoucher.VehicleId = SelectedVehicle.VehicleId;

        // Enhanced validation with better error messages
        if (CurrentCompany == null)
        {
            StatusMessage = "❌ Company must be selected";
            return Task.FromResult(false);
        }

        // Simple vehicle validation
        if (SelectedVehicle == null)
        {
            if (!string.IsNullOrWhiteSpace(VehicleSearchText))
            {
                StatusMessage = $"❌ Vehicle '{VehicleSearchText}' not found. Go to VEHICLE MANAGEMENT (F1) to create it first.";
            }
            else
            {
                StatusMessage = "❌ Please select a vehicle.";
            }
            return Task.FromResult(false);
        }

        if (CurrentVoucher.Amount <= 0)
        {
            StatusMessage = "❌ Amount must be greater than 0";
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(CurrentVoucher.DrCr) || (CurrentVoucher.DrCr != "D" && CurrentVoucher.DrCr != "C"))
        {
            StatusMessage = "❌ Please select Dr/Cr";
            return Task.FromResult(false);
        }

        StatusMessage = $"✅ Validation successful - Ready to save voucher #{CurrentVoucher.VoucherNumber}: ₹{CurrentVoucher.Amount:N2} for {SelectedVehicle.DisplayName}";
        return Task.FromResult(true);
    }
    

    #region Commands

    /// <summary>
    /// Creates a new voucher (F2)
    /// </summary>
    [RelayCommand]
    private async Task NewVoucher()
    {
        // Store current voucher info before initializing new one
        var currentVoucherNumber = CurrentVoucher?.VoucherNumber ?? 0;
        var isCurrentVoucherUnsaved = CurrentVoucher?.VoucherId == 0;

        // Remember the Dr/Cr selection from current voucher
        if (CurrentVoucher != null && !string.IsNullOrEmpty(CurrentVoucher.DrCr))
        {
            _lastSelectedDrCr = CurrentVoucher.DrCr;
        }

        InitializeNewVoucher();

        if (isCurrentVoucherUnsaved && currentVoucherNumber > 0)
        {
            // Manually increment the voucher number since the previous one wasn't saved
            var nextNumber = currentVoucherNumber + 1;
            if (CurrentCompany != null)
            {
                // Update the company's in-memory last voucher number to reflect the increment
                CurrentCompany.LastVoucherNumber = Math.Max(CurrentCompany.LastVoucherNumber, nextNumber - 1);
            }
            if (CurrentVoucher != null)
            {
                CurrentVoucher.VoucherNumber = nextNumber;
            }
            StatusMessage = $"Ready for voucher #{nextNumber}";
        }
        else
        {
            await SetNextVoucherNumberAsync();
        }
    }

    /// <summary>
    /// Saves the current voucher (F5)
    /// </summary>
    [RelayCommand]
    private async Task SaveVoucher()
    {
        if (!await ValidateCurrentVoucherAsync())
            return;

        await ExecuteAsync(async () =>
        {
            
            Voucher savedVoucher;
            bool isNewVoucher = CurrentVoucher.VoucherId == 0;

            if (isNewVoucher)
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

            // Only prepare for next voucher if we just created a new voucher
            // Don't reset the form when updating an existing voucher
            if (isNewVoucher)
            {
                // Remember the Dr/Cr selection for next voucher
                _lastSelectedDrCr = CurrentVoucher.DrCr;

                // Prepare for next voucher after successful save of new voucher
                InitializeNewVoucher();
                await SetNextVoucherNumberAsync();

                // Ask view to focus the vehicle search for speedy entry
                FocusVehicleSearchRequested?.Invoke();
            }

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
    /// Refreshes the voucher and vehicle data
    /// </summary>
    [RelayCommand]
    private async Task RefreshData()
    {
        await LoadDataAsync();
        StatusMessage = "Data refreshed successfully - vehicles and vouchers reloaded";
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

            // Update selected date to match the voucher's date
            SelectedDate = value.Date;
            _lastSelectedDate = value.Date;

            // Update entered amount to match the voucher's amount
            EnteredAmount = value.Amount;

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
            // Keep search box in sync with selection
            VehicleSearchText = value.DisplayName;
            ShowVehicleSuggestions = false;
        }
    }

    /// <summary>
    /// Handles current voucher changes to persist date selection
    /// </summary>
    partial void OnCurrentVoucherChanged(Voucher value)
    {
        if (value != null && value.Date != default(DateTime))
        {
            _lastSelectedDate = value.Date;
            SelectedDate = value.Date;
        }
    }

    /// <summary>
    /// Handles date picker changes to update voucher date and persist selection
    /// </summary>
    partial void OnSelectedDateChanged(DateTime value)
    {
        if (CurrentVoucher != null)
        {
            CurrentVoucher.Date = value;
            _lastSelectedDate = value;
        }
    }

    /// <summary>
    /// Handles amount entry changes to update voucher amount
    /// </summary>
    partial void OnEnteredAmountChanged(decimal? value)
    {
        if (CurrentVoucher != null)
        {
            CurrentVoucher.Amount = value ?? 0m;
        }
    }

    partial void OnVehicleSearchTextChanged(string value)
    {
        UpdateVehicleFilter();
        ShowVehicleSuggestions = !string.IsNullOrWhiteSpace(VehicleSearchText);

        var searchTerm = VehicleSearchText?.Trim();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            SelectedVehicle = null;
            StatusMessage = "Ready to enter vouchers";
            return;
        }

        // Simple exact match only - if vehicle exists, select it immediately
        var exactMatch = Vehicles.FirstOrDefault(v =>
            string.Equals(v.VehicleNumber?.Trim(), searchTerm, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            SelectedVehicle = exactMatch;
            StatusMessage = $"Vehicle selected: {exactMatch.DisplayName}";
        }
        else
        {
            // Don't clear SelectedVehicle immediately - user might still be typing
            StatusMessage = $"Typing '{searchTerm}'...";
        }
    }


    private void UpdateVehicleFilter()
    {
        var term = (VehicleSearchText ?? string.Empty).Trim();
        FilteredVehicles.Clear();

        if (string.IsNullOrWhiteSpace(term))
        {
            // Show a small recent/top subset to help discovery
            foreach (var v in Vehicles.Take(10))
                FilteredVehicles.Add(v);
            return;
        }

        var hit = Vehicles
            .Where(v => MatchesSearchTerm(v, term))
            .OrderBy(v => GetSearchRelevanceScore(v, term))
            .Take(20);

        foreach (var v in hit)
            FilteredVehicles.Add(v);

        // If no exact matches and user has typed something, show option to create new vehicle
        if (!FilteredVehicles.Any() && !string.IsNullOrWhiteSpace(term))
        {
            // Add a placeholder vehicle to represent "Create New Vehicle" option
            var newVehiclePlaceholder = new Vehicle
            {
                VehicleId = -1, // Use negative ID to indicate this is a placeholder
                VehicleNumber = $"+ Create '{term}'",
                Description = "Press Enter to create this new vehicle",
                CompanyId = CurrentCompany?.CompanyId ?? 0,
                IsActive = true
            };
            FilteredVehicles.Add(newVehiclePlaceholder);
        }
    }

    /// <summary>
    /// Enhanced search matching that supports partial vehicle numbers and fuzzy matching
    /// </summary>
    private bool MatchesSearchTerm(Vehicle vehicle, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return true;
        
        var term = searchTerm.ToLowerInvariant();
        
        // Check exact matches first (highest priority)
        if (vehicle.VehicleNumber?.ToLowerInvariant().Contains(term) == true ||
            vehicle.DisplayName?.ToLowerInvariant().Contains(term) == true ||
            vehicle.Description?.ToLowerInvariant().Contains(term) == true)
        {
            return true;
        }
        
        // Check if search term could be a partial vehicle number (remove common separators)
        var cleanVehicleNumber = vehicle.VehicleNumber?.Replace("-", "").Replace(" ", "").Replace(".", "").ToLowerInvariant();
        var cleanSearchTerm = term.Replace("-", "").Replace(" ", "").Replace(".", "");
        
        if (cleanVehicleNumber?.Contains(cleanSearchTerm) == true)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculate search relevance score (lower is better)
    /// </summary>
    private int GetSearchRelevanceScore(Vehicle vehicle, string searchTerm)
    {
        var term = searchTerm.ToLowerInvariant();
        int score = 100; // Base score
        
        // Exact vehicle number match gets highest priority (lowest score)
        if (vehicle.VehicleNumber?.ToLowerInvariant() == term)
            return 1;
            
        // Vehicle number starts with search term gets high priority
        if (vehicle.VehicleNumber?.ToLowerInvariant().StartsWith(term) == true)
            return 2;
            
        // Vehicle number contains search term
        if (vehicle.VehicleNumber?.ToLowerInvariant().Contains(term) == true)
            score -= 30;
            
        // Description contains search term
        if (vehicle.Description?.ToLowerInvariant().Contains(term) == true)
            score -= 10;
            
        return score;
    }

    [RelayCommand]
    private void SelectVehicle(Vehicle vehicle)
    {
        SelectedVehicle = vehicle;
        VehicleSearchText = vehicle.VehicleNumber; // Use vehicle number instead of DisplayName
        ShowVehicleSuggestions = false;
        StatusMessage = $"Vehicle selected: {vehicle.DisplayName}";

        // Auto-focus the amount field after vehicle selection
        FocusAmountFieldRequested?.Invoke();
    }

    /// <summary>
    /// Handles Enter key press in vehicle search box - simple vehicle selection
    /// </summary>
    [RelayCommand]
    private void HandleVehicleSearchEnter()
    {
        if (string.IsNullOrWhiteSpace(VehicleSearchText) || CurrentCompany == null)
            return;

        var searchTerm = VehicleSearchText.Trim();

        // Simple: if we already have a selected vehicle, confirm it
        if (SelectedVehicle != null)
        {
            StatusMessage = $"Vehicle confirmed: {SelectedVehicle.DisplayName}";
            ShowVehicleSuggestions = false;

            // Auto-focus the amount field when vehicle is confirmed
            FocusAmountFieldRequested?.Invoke();
            return;
        }

        // Try to find exact match
        var exactMatch = Vehicles.FirstOrDefault(v =>
            string.Equals(v.VehicleNumber?.Trim(), searchTerm, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            SelectVehicle(exactMatch);
            return;
        }

        // Check if first filtered vehicle is valid
        if (FilteredVehicles.Any() && !FilteredVehicles.First().VehicleNumber.StartsWith("+ Create"))
        {
            SelectVehicle(FilteredVehicles.First());
            return;
        }

        // Vehicle not found
        StatusMessage = $"Vehicle '{searchTerm}' not found. Go to VEHICLE MANAGEMENT (F1) to create it first.";
        SelectedVehicle = null;
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

            // Always refresh data when navigating to VoucherEntry (including when returning from other views)
            await LoadDataAsync();
            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: LoadDataAsync completed");

            // Initialize a new voucher with the company set only if we don't have a current voucher being edited
            if (CurrentVoucher?.VoucherId == 0 || CurrentVoucher == null)
            {
                InitializeNewVoucher();
                await SetNextVoucherNumberAsync();
                System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: Voucher initialization completed");
            }
            else
            {
                // Maintain the current date when returning to voucher entry
                System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: Maintaining existing voucher and date");
            }
        }
        else if (CurrentCompany != null)
        {
            // No parameters passed but we have a current company - refresh data (e.g., returning from Vehicle Management)
            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: No Company parameter but CurrentCompany exists - refreshing data");
            await LoadDataAsync();

            // Don't initialize a new voucher if we already have one being edited - preserve the date and all data
            if (CurrentVoucher?.VoucherId == 0 || CurrentVoucher == null)
            {
                InitializeNewVoucher();
                await SetNextVoucherNumberAsync();
                System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: New voucher initialized");
            }
            else
            {
                // Preserve existing voucher data including the selected date
                System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: Preserving existing voucher data and date");
            }

            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: Data refresh completed");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"VoucherEntryViewModel.OnNavigatedToAsync: No Company parameter and no CurrentCompany found");
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
