using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Linq;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// ViewModel for the search screen with dynamic vehicle search and voucher management
/// </summary>
public partial class SearchViewModel : BaseViewModel, INavigationAware
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareTransactionsCommand))]
    private ObservableCollection<Voucher> _vouchers = new();

    [ObservableProperty]
    private ObservableCollection<VehicleDisplayItem> _vehicleSearchResults = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareTransactionsCommand))]
    private VehicleDisplayItem? _selectedVehicle;

    [ObservableProperty]
    private VehicleDisplayItem? _selectedVehicleFromSearch;

    [ObservableProperty]
    private Voucher? _selectedVoucher;

    private Voucher _currentVoucher = new();
    
    public Voucher CurrentVoucher
    {
        get => _currentVoucher;
        set
        {
            // Unsubscribe from previous voucher's property changes
            if (_currentVoucher != null)
            {
                _currentVoucher.PropertyChanged -= OnCurrentVoucherPropertyChanged;
            }
            
            if (SetProperty(ref _currentVoucher, value))
            {
                // Subscribe to new voucher's property changes
                if (_currentVoucher != null)
                {
                    _currentVoucher.PropertyChanged += OnCurrentVoucherPropertyChanged;
                }
                
                UpdateVoucherCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Handles property changes in the current voucher to update button state
    /// </summary>
    private void OnCurrentVoucherPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Key properties that affect whether update button should be enabled
        if (e.PropertyName == nameof(Voucher.Amount) ||
            e.PropertyName == nameof(Voucher.DrCr) ||
            e.PropertyName == nameof(Voucher.Narration))
        {
            UpdateVoucherCommand.NotifyCanExecuteChanged();
        }
    }

    [ObservableProperty]
    private Company? _currentCompany;

    [ObservableProperty]
    private string _statusMessage = "Enter vehicle name to search for vouchers";

    [ObservableProperty]
    private string _vehicleSearchTerm = string.Empty;

    [ObservableProperty]
    private bool _isVehicleSearchOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateVoucherCommand))]
    private bool _isVoucherEditMode;

    [ObservableProperty]
    private int _totalVouchers;

    [ObservableProperty]
    private ObservableCollection<VehicleDisplayItem> _availableVehicles = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateVoucherCommand))]
    private VehicleDisplayItem? _selectedVehicleForEdit;

    private List<VehicleDisplayItem> _allVehicles = new();
    private List<Voucher> _allVouchers = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareTransactionsCommand))]
    private bool _isComparisonActive;

    [ObservableProperty]
    private int _unmatchedDebitsCount;

    [ObservableProperty]
    private int _unmatchedCreditsCount;

    [ObservableProperty]
    private bool _showOnlyUnmatched;

    public SearchViewModel(DataService dataService) : base(dataService)
    {
        InitializeNewVoucher();
    }

    /// <summary>
    /// Loads initial data for the search screen
    /// </summary>
    public async Task LoadDataAsync()
    {
        if (CurrentCompany == null) 
        {
            StatusMessage = "❌ No company selected for search";
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Load all vehicles with balance information for search
            var vehicles = await _dataService.Vehicles.GetByCompanyIdAsync(CurrentCompany.CompanyId);
            var vehicleDisplayItems = new List<VehicleDisplayItem>();

            StatusMessage = $"🔄 Loading {vehicles.Count()} vehicles for company {CurrentCompany.Name}...";

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
            
            // Update available vehicles for editing
            AvailableVehicles.Clear();
            foreach (var vehicle in _allVehicles.OrderBy(v => v.VehicleNumber))
            {
                AvailableVehicles.Add(vehicle);
            }
            
            StatusMessage = $"✅ Loaded {_allVehicles.Count} vehicles. Type vehicle number to search.";

        }, "Loading vehicles...");
    }

    /// <summary>
    /// Loads vouchers for the selected vehicle with optimized performance
    /// </summary>
    private async Task LoadVouchersForVehicleAsync(VehicleDisplayItem vehicle)
    {
        const int INITIAL_PAGE_SIZE = 500;
        
        try
        {
            StatusMessage = $"🔄 Loading vouchers for {vehicle.VehicleNumber}...";
            
            // Validation
            if (vehicle.VehicleId <= 0)
            {
                throw new InvalidOperationException($"Invalid vehicle ID: {vehicle.VehicleId}");
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Use background task for heavy work
            var (vouchers, totalCount) = await Task.Run(async () =>
            {
                // First, get count to determine if we need pagination
                var result = await _dataService.Vouchers.GetByVehicleIdPagedAsync(vehicle.VehicleId, INITIAL_PAGE_SIZE, 0);
                return result;
            });
            
            // Convert to list only once, in background
            _allVouchers = await Task.Run(() => vouchers.ToList());
            
            stopwatch.Stop();
            StatusMessage = $"📊 Retrieved {_allVouchers.Count} of {totalCount} vouchers in {stopwatch.ElapsedMilliseconds}ms...";
            
            // Update UI efficiently with batch operation
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // Use CollectionChanged suspension for better performance
                Vouchers.Clear();
                
                // For large datasets, add items in smaller batches to keep UI responsive
                if (_allVouchers.Count > 100)
                {
                    StatusMessage = $"🔄 Updating display ({_allVouchers.Count} items)...";
                    
                    // Add items in batches to prevent UI freezing
                    var batchSize = 50;
                    for (int i = 0; i < _allVouchers.Count; i += batchSize)
                    {
                        var batch = _allVouchers.Skip(i).Take(batchSize);
                        foreach (var voucher in batch)
                        {
                            Vouchers.Add(voucher);
                        }
                        
                        // Allow UI to process updates periodically
                        if (i % 200 == 0)
                        {
                            await Task.Delay(1); // Allow UI to process
                        }
                    }
                }
                else
                {
                    // For smaller datasets, add all at once
                    foreach (var voucher in _allVouchers)
                    {
                        Vouchers.Add(voucher);
                    }
                }

                TotalVouchers = _allVouchers.Count;

                // Notify that comparison button can be enabled now
                CompareTransactionsCommand.NotifyCanExecuteChanged();
            });

            // Update status with performance metrics
            var displayedCount = Vouchers.Count;
            if (totalCount > INITIAL_PAGE_SIZE && displayedCount < totalCount)
            {
                StatusMessage = $"✅ Showing first {displayedCount} of {totalCount} vouchers for {vehicle.VehicleNumber}. Scroll for more.";
            }
            else if (displayedCount == 0)
            {
                StatusMessage = $"❌ No vouchers found for {vehicle.DisplayName}. Balance: {vehicle.FormattedBalance}";
            }
            else
            {
                var latestBalance = _allVouchers.FirstOrDefault()?.RunningBalance ?? 0;
                StatusMessage = $"✅ Loaded {displayedCount} vouchers for {vehicle.DisplayName} in {stopwatch.ElapsedMilliseconds}ms. Balance: ₹{latestBalance:N2}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❗ Error loading vouchers for {vehicle.VehicleNumber}: {ex.Message}";
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Vouchers.Clear();
                TotalVouchers = 0;
            });
            System.Diagnostics.Debug.WriteLine($"LoadVouchersForVehicleAsync error: {ex}");
        }
    }

    /// <summary>
    /// Calculates running balances for vouchers (Legacy method - now done in SQL for performance)
    /// </summary>
    private void CalculateRunningBalances()
    {
        // This method is kept for compatibility but running balances are now calculated in SQL
        // for much better performance with large datasets
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
            StatusMessage = $"✅ Loaded {_allVehicles.Count} vehicles. Type vehicle number to search.";
            return;
        }

        var term = VehicleSearchTerm.ToLowerInvariant();
        var filtered = _allVehicles
            .Where(v => MatchesVehicleSearchTerm(v, term))
            .OrderBy(v => GetVehicleSearchRelevanceScore(v, term))
            .Take(20);

        foreach (var vehicle in filtered)
        {
            VehicleSearchResults.Add(vehicle);
        }

        IsVehicleSearchOpen = VehicleSearchResults.Count > 0;
        
        if (VehicleSearchResults.Count > 0)
        {
            StatusMessage = $"🔍 Found {VehicleSearchResults.Count} matches for '{VehicleSearchTerm}'";
        }
        else if (!string.IsNullOrWhiteSpace(VehicleSearchTerm))
        {
            StatusMessage = $"❌ No vehicles found matching '{VehicleSearchTerm}'";
        }
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
        try
        {
            await LoadDataAsync();
            if (SelectedVehicle != null)
            {
                StatusMessage = $"🔄 Refreshing vouchers for {SelectedVehicle.VehicleNumber}...";
                await LoadVouchersForVehicleAsync(SelectedVehicle);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❗ Error refreshing data: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"RefreshData error: {ex}");
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

        // Set the selected vehicle for editing
        SelectedVehicleForEdit = _allVehicles.FirstOrDefault(v => v.VehicleId == voucher.VehicleId);

        IsVoucherEditMode = true;
        StatusMessage = $"Editing voucher: {voucher.VoucherNumber} - You can change vehicle if needed";
    }

    /// <summary>
    /// Updates the current voucher
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUpdateVoucher))]
    private async Task UpdateVoucher()
    {
        try
        {
            // Validation checks
            if (CurrentVoucher == null || CurrentVoucher.VoucherId == 0)
            {
                StatusMessage = "No voucher selected for update";
                return;
            }

            if (CurrentVoucher.Amount <= 0)
            {
                StatusMessage = "Amount must be greater than zero";
                return;
            }

            if (SelectedVehicleForEdit == null)
            {
                StatusMessage = "Please select a vehicle";
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentVoucher.DrCr))
            {
                StatusMessage = "Please select Dr or Cr";
                return;
            }

            // Narration is optional for updates - allow empty narration

            await ExecuteAsync(async () =>
            {
                // Store original vehicle info for comparison
                var originalVehicleId = CurrentVoucher.VehicleId;
                var originalVehicleNumber = CurrentVoucher.Vehicle?.VehicleNumber ?? "Unknown";
                
                // Update the voucher's vehicle
                CurrentVoucher.VehicleId = SelectedVehicleForEdit.VehicleId;
                CurrentVoucher.Vehicle = new Vehicle
                {
                    VehicleId = SelectedVehicleForEdit.VehicleId,
                    VehicleNumber = SelectedVehicleForEdit.VehicleNumber,
                    Narration = SelectedVehicleForEdit.Description
                };
                
                // Update the voucher in database
                var updatedVoucher = await _dataService.Vouchers.UpdateAsync(CurrentVoucher);
                
                // Show success message
                if (originalVehicleId != SelectedVehicleForEdit.VehicleId)
                {
                    StatusMessage = $"Voucher '{updatedVoucher.VoucherNumber}' updated successfully - Vehicle changed from {originalVehicleNumber} to {SelectedVehicleForEdit.VehicleNumber}";
                }
                else
                {
                    StatusMessage = $"Voucher '{updatedVoucher.VoucherNumber}' updated successfully";
                }
                
                // Exit edit mode and refresh
                IsVoucherEditMode = false;
                SelectedVehicleForEdit = null;
                InitializeNewVoucher();
                
                // Refresh the data to show updated information
                await RefreshData();

            }, "Updating voucher...");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating voucher: {ex.Message}";
        }
    }

    /// <summary>
    /// Determines if voucher can be updated
    /// </summary>
    private bool CanUpdateVoucher()
    {
        return IsVoucherEditMode &&
               CurrentVoucher != null &&
               CurrentVoucher.VoucherId > 0 &&
               SelectedVehicleForEdit != null &&
               CurrentVoucher.Amount > 0 &&
               !string.IsNullOrWhiteSpace(CurrentVoucher.DrCr);
               // Narration is optional - removed the narration check
    }

    /// <summary>
    /// Cancels voucher editing
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsVoucherEditMode = false;
        SelectedVehicleForEdit = null;
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
                    SelectedVehicleForEdit = null;
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

    /// <summary>
    /// Compares credit and debit transactions to find unmatched entries
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCompareTransactions))]
    private async Task CompareTransactions()
    {
        if (SelectedVehicle == null) return;

        await ExecuteAsync(async () =>
        {
            // Get all vouchers for the selected vehicle
            var allVouchers = await _dataService.Vouchers.GetByVehicleIdAsync(SelectedVehicle.VehicleId);
            var voucherList = allVouchers.ToList();

            // Separate debits and credits
            var debits = voucherList.Where(v => v.DrCr == "D").ToList();
            var credits = voucherList.Where(v => v.DrCr == "C").ToList();

            // Group by amount for matching
            var debitAmounts = debits.GroupBy(v => v.Amount).ToDictionary(g => g.Key, g => g.ToList());
            var creditAmounts = credits.GroupBy(v => v.Amount).ToDictionary(g => g.Key, g => g.ToList());

            // Track which vouchers have been matched
            var matchedDebitIds = new HashSet<int>();
            var matchedCreditIds = new HashSet<int>();

            // Match debits with credits by amount (one-to-one pairing)
            foreach (var debitGroup in debitAmounts)
            {
                var amount = debitGroup.Key;
                var debitVouchers = debitGroup.Value;

                if (creditAmounts.TryGetValue(amount, out var creditVouchers))
                {
                    // Match as many as possible (one-to-one)
                    int matchCount = Math.Min(debitVouchers.Count, creditVouchers.Count);

                    for (int i = 0; i < matchCount; i++)
                    {
                        matchedDebitIds.Add(debitVouchers[i].VoucherId);
                        matchedCreditIds.Add(creditVouchers[i].VoucherId);
                    }
                }
            }

            // Mark unmatched vouchers in the current view
            int unmatchedDebits = 0;
            int unmatchedCredits = 0;

            foreach (var voucher in Vouchers)
            {
                if (voucher.DrCr == "D" && !matchedDebitIds.Contains(voucher.VoucherId))
                {
                    voucher.IsUnmatched = true;
                    unmatchedDebits++;
                }
                else if (voucher.DrCr == "C" && !matchedCreditIds.Contains(voucher.VoucherId))
                {
                    voucher.IsUnmatched = true;
                    unmatchedCredits++;
                }
                else
                {
                    voucher.IsUnmatched = false;
                }
            }

            UnmatchedDebitsCount = unmatchedDebits;
            UnmatchedCreditsCount = unmatchedCredits;
            IsComparisonActive = true;

            if (unmatchedDebits == 0 && unmatchedCredits == 0)
            {
                StatusMessage = $"✓ All transactions matched for {SelectedVehicle.VehicleNumber}";
            }
            else
            {
                StatusMessage = $"⚠ Comparison active: {unmatchedDebits} unmatched debits, {unmatchedCredits} unmatched credits";
            }

        }, "Comparing transactions...");
    }

    /// <summary>
    /// Determines if comparison can be performed
    /// </summary>
    private bool CanCompareTransactions()
    {
        return SelectedVehicle != null && Vouchers.Count > 0 && !IsComparisonActive;
    }

    /// <summary>
    /// Clears the comparison highlighting
    /// </summary>
    [RelayCommand]
    private void ClearComparison()
    {
        foreach (var voucher in Vouchers)
        {
            voucher.IsUnmatched = false;
        }

        IsComparisonActive = false;
        UnmatchedDebitsCount = 0;
        UnmatchedCreditsCount = 0;

        if (SelectedVehicle != null)
        {
            StatusMessage = $"Showing all vouchers for {SelectedVehicle.VehicleNumber}";
        }
        else
        {
            StatusMessage = "Enter vehicle name to search for vouchers";
        }

        ShowOnlyUnmatched = false;
        CompareTransactionsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Toggles filter to show only unmatched transactions
    /// </summary>
    [RelayCommand]
    private void ToggleUnmatchedFilter()
    {
        ShowOnlyUnmatched = !ShowOnlyUnmatched;
        ApplyUnmatchedFilter();
    }

    /// <summary>
    /// Applies the unmatched filter to the vouchers collection
    /// </summary>
    private void ApplyUnmatchedFilter()
    {
        if (ShowOnlyUnmatched)
        {
            // Filter to show only unmatched
            var unmatched = _allVouchers.Where(v => v.IsUnmatched).ToList();
            Vouchers.Clear();
            foreach (var v in unmatched)
            {
                Vouchers.Add(v);
            }
            StatusMessage = $"🔍 Showing {unmatched.Count} unmatched transactions ({UnmatchedDebitsCount} debits, {UnmatchedCreditsCount} credits)";
        }
        else
        {
            // Show all vouchers
            Vouchers.Clear();
            foreach (var v in _allVouchers)
            {
                Vouchers.Add(v);
            }
            StatusMessage = $"⚠ Comparison active: {UnmatchedDebitsCount} unmatched debits, {UnmatchedCreditsCount} unmatched credits";
        }
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
            // Properly handle async operation with error handling
            Task.Run(async () =>
            {
                try
                {
                    await LoadVouchersForVehicleAsync(value);
                }
                catch (Exception ex)
                {
                    // Update status on main thread
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = $"Error loading vouchers for {value.VehicleNumber}: {ex.Message}";
                        Vouchers.Clear();
                        TotalVouchers = 0;
                    });
                }
            });
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

    #region Private Helper Methods

    /// <summary>
    /// Enhanced search matching that supports flexible vehicle number typing
    /// </summary>
    private bool MatchesVehicleSearchTerm(VehicleDisplayItem vehicle, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return true;
        
        var term = searchTerm.ToLowerInvariant().Trim();
        var vehicleNumber = vehicle.VehicleNumber.ToLowerInvariant();
        var description = vehicle.Description?.ToLowerInvariant() ?? "";
        
        // 1. Exact match (highest priority)
        if (vehicleNumber == term || description == term)
            return true;
            
        // 2. Starts with match (very high priority)
        if (vehicleNumber.StartsWith(term) || description.StartsWith(term))
            return true;
            
        // 3. Contains match (high priority)
        if (vehicleNumber.Contains(term) || description.Contains(term))
            return true;
            
        // 4. Clean matching - remove all separators and spaces
        var cleanVehicle = CleanVehicleNumber(vehicleNumber);
        var cleanTerm = CleanVehicleNumber(term);
        
        if (cleanVehicle.Contains(cleanTerm))
            return true;
            
        // 5. Flexible pattern matching for common vehicle number formats
        if (MatchesVehiclePattern(vehicleNumber, term))
            return true;
            
        // 6. Word-based matching for descriptions
        if (MatchesWordBased(description, term))
            return true;
            
        // 7. Number sequence matching (e.g., "1234" matches "AB-1234-CD")
        if (MatchesNumberSequence(vehicleNumber, term))
            return true;
            
        return false;
    }
    
    /// <summary>
    /// Cleans vehicle number by removing all non-alphanumeric characters
    /// </summary>
    private string CleanVehicleNumber(string input)
    {
        return new string(input.Where(char.IsLetterOrDigit).ToArray());
    }
    
    /// <summary>
    /// Matches flexible vehicle number patterns
    /// </summary>
    private bool MatchesVehiclePattern(string vehicleNumber, string searchTerm)
    {
        // Handle common patterns like:
        // Search: "hr26" should match "HR-26-1234", "HR26AB1234", etc.
        // Search: "1234" should match "HR-26-1234", "AB1234CD", etc.
        
        var cleanVehicle = CleanVehicleNumber(vehicleNumber);
        var cleanTerm = CleanVehicleNumber(searchTerm);
        
        // If term is numeric, try to match number sequences
        if (cleanTerm.All(char.IsDigit) && cleanTerm.Length >= 2)
        {
            var vehicleNumbers = new string(cleanVehicle.Where(char.IsDigit).ToArray());
            if (vehicleNumbers.Contains(cleanTerm))
                return true;
        }
        
        // If term contains letters and numbers, match in sequence
        if (cleanTerm.Any(char.IsLetter) && cleanTerm.Any(char.IsDigit))
        {
            return cleanVehicle.Contains(cleanTerm);
        }
        
        // Letter-only matching (state codes, etc.)
        if (cleanTerm.All(char.IsLetter) && cleanTerm.Length >= 2)
        {
            var vehicleLetters = new string(cleanVehicle.Where(char.IsLetter).ToArray());
            if (vehicleLetters.Contains(cleanTerm))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Matches word-based search in descriptions
    /// </summary>
    private bool MatchesWordBased(string description, string searchTerm)
    {
        if (string.IsNullOrEmpty(description)) return false;
        
        var words = description.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Any(word => word.StartsWith(searchTerm) || word.Contains(searchTerm));
    }
    
    /// <summary>
    /// Matches number sequences in vehicle numbers
    /// </summary>
    private bool MatchesNumberSequence(string vehicleNumber, string searchTerm)
    {
        if (!searchTerm.All(char.IsDigit) || searchTerm.Length < 2)
            return false;
            
        // Extract all numeric sequences from vehicle number
        var numbers = System.Text.RegularExpressions.Regex.Matches(vehicleNumber, @"\d+")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => m.Value)
            .ToList();
            
        return numbers.Any(num => num.Contains(searchTerm) || num.StartsWith(searchTerm));
    }
    
    /// <summary>
    /// Calculate search relevance score (lower is better) with enhanced prioritization
    /// </summary>
    private int GetVehicleSearchRelevanceScore(VehicleDisplayItem vehicle, string searchTerm)
    {
        var term = searchTerm.ToLowerInvariant().Trim();
        var vehicleNumber = vehicle.VehicleNumber.ToLowerInvariant();
        var description = vehicle.Description?.ToLowerInvariant() ?? "";
        
        // Exact matches (highest priority)
        if (vehicleNumber == term) return 1;
        if (description == term) return 2;
        
        // Starts with matches (very high priority)
        if (vehicleNumber.StartsWith(term)) return 3;
        if (description.StartsWith(term)) return 4;
        
        // Clean exact match
        var cleanVehicle = CleanVehicleNumber(vehicleNumber);
        var cleanTerm = CleanVehicleNumber(term);
        if (cleanVehicle == cleanTerm) return 5;
        if (cleanVehicle.StartsWith(cleanTerm)) return 6;
        
        int score = 100; // Base score for other matches
        
        // Vehicle number contains term (high priority)
        if (vehicleNumber.Contains(term))
            score -= 40;
            
        // Clean vehicle number contains clean term
        if (cleanVehicle.Contains(cleanTerm))
            score -= 35;
            
        // Number sequence matching
        if (term.All(char.IsDigit) && term.Length >= 2)
        {
            var vehicleNumbers = new string(cleanVehicle.Where(char.IsDigit).ToArray());
            if (vehicleNumbers.StartsWith(term))
                score -= 30;
            else if (vehicleNumbers.Contains(term))
                score -= 25;
        }
        
        // Letter sequence matching (state codes)
        if (term.All(char.IsLetter) && term.Length >= 2)
        {
            var vehicleLetters = new string(cleanVehicle.Where(char.IsLetter).ToArray());
            if (vehicleLetters.StartsWith(term))
                score -= 28;
            else if (vehicleLetters.Contains(term))
                score -= 23;
        }
        
        // Description matches (lower priority)
        if (description.Contains(term))
            score -= 15;
            
        // Word-based description matches
        var words = description.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(word => word.StartsWith(term)))
            score -= 12;
        else if (words.Any(word => word.Contains(term)))
            score -= 8;
            
        return score;
    }

    #endregion
}
