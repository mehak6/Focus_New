using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using FocusVoucherSystem.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using System.Linq;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// ViewModel for the main application window
/// </summary>
public partial class MainWindowViewModel : BaseViewModel, INavigationAware
{
    private readonly NavigationService _navigationService;
    private readonly HotkeyService _hotkeyService;

    [ObservableProperty]
    private string _title = "Focus Voucher System";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private Company? _currentCompany;

    [ObservableProperty]
    private string _currentUser = Environment.UserName;

    [ObservableProperty]
    private ObservableCollection<Company> _companies = new();

    [ObservableProperty]
    private string _selectedTab = "VoucherEntry";

    public MainWindowViewModel(DataService dataService, NavigationService navigationService, HotkeyService hotkeyService) 
        : base(dataService)
    {
        _navigationService = navigationService;
        _hotkeyService = hotkeyService;
        
        RegisterHotkeys();
    }

    /// <summary>
    /// Initializes the main window (simplified version)
    /// </summary>
    public async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load companies for the dropdown
            await LoadCompaniesAsync();

            StatusMessage = "Application initialized successfully";

        }, "Initializing application...");
    }

    /// <summary>
    /// Loads all companies from the database
    /// </summary>
    private async Task LoadCompaniesAsync()
    {
        var companies = await _dataService.Companies.GetActiveCompaniesAsync();
        Companies.Clear();
        foreach (var company in companies)
        {
            Companies.Add(company);
        }
    }

    /// <summary>
    /// Checks if any companies exist and prompts user to create one if none exist
    /// </summary>
    private async Task<bool> EnsureCompanyExistsAsync()
    {
        StatusMessage = $"Checking companies... Found {Companies.Count} companies";
        
        if (Companies.Any())
        {
            StatusMessage = $"Companies exist: {string.Join(", ", Companies.Select(c => c.Name))}";
            return true; // Companies already exist
        }

        StatusMessage = "No companies found - showing creation dialog";

        // No companies found, prompt user to create one
        var result = MessageBox.Show(
            "No companies found in the database. Would you like to create a new company?",
            "Create Company",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            // Prompt for company name using custom dialog
            var inputDialog = new Views.CompanyInputDialog();
            var dialogResult = inputDialog.ShowDialog();

            if (dialogResult == true && !string.IsNullOrWhiteSpace(inputDialog.CompanyName))
            {
                await CreateInitialCompanyAsync(inputDialog.CompanyName);
                return true;
            }
            else
            {
                MessageBox.Show("Company creation cancelled. Application will close.",
                    "No Company Created", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        else
        {
            MessageBox.Show("Cannot proceed without a company. Application will close.",
                "No Company Created", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// Creates the initial company with default settings
    /// </summary>
    private async Task CreateInitialCompanyAsync(string companyName)
    {
        try
        {
            StatusMessage = $"Creating company: {companyName}";
            
            var currentDate = DateTime.Now;
            var company = new Company
            {
                Name = companyName,
                FinancialYearStart = new DateTime(currentDate.Year, 4, 1), // April 1st
                FinancialYearEnd = new DateTime(currentDate.Year + 1, 3, 31), // March 31st next year
                LastVoucherNumber = 0,
                IsActive = true
            };

            var createdCompany = await _dataService.Companies.AddAsync(company);
            Companies.Add(createdCompany);

            // Set as current company
            CurrentCompany = createdCompany;
            Title = $"Focus Voucher System - {companyName}";

            // Save as default company
            await _dataService.Settings.SetValueAsync("DefaultCompanyId", createdCompany.CompanyId);

            StatusMessage = $"Successfully created company: {companyName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create company: {ex.Message}";
            MessageBox.Show($"Failed to create company: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            throw; // Re-throw to stop application startup
        }
    }

    /// <summary>
    /// Sets the default company from settings
    /// </summary>
    private async Task SetDefaultCompanyAsync()
    {
        var defaultCompanyId = await _dataService.Settings.GetValueAsync<int>("DefaultCompanyId", 1);
        var company = await _dataService.Companies.GetByIdAsync(defaultCompanyId);
        
        if (company != null)
        {
            CurrentCompany = company;
            Title = $"Focus Voucher System - {company.Name}";
        }
        else if (Companies.Any())
        {
            CurrentCompany = Companies.First();
            Title = $"Focus Voucher System - {CurrentCompany.Name}";
        }
    }

    /// <summary>
    /// Registers global hotkeys
    /// </summary>
    private void RegisterHotkeys()
    {
        _hotkeyService.RegisterHotkey(Key.F1, VehicleManagementCommand, "Vehicle Number management");
        _hotkeyService.RegisterHotkey(Key.F2, AddVoucherCommand, "Add new voucher/row");
        _hotkeyService.RegisterHotkey(Key.F3, NavigateToSearchCommand, "Search vouchers by vehicle");
        _hotkeyService.RegisterHotkey(Key.F4, ReportsCommand, "Reports menu");
        _hotkeyService.RegisterHotkey(Key.F5, SaveCommand, "Save current operation");
        _hotkeyService.RegisterHotkey(Key.F8, DeleteCommand, "Delete selected item");
        _hotkeyService.RegisterHotkey(Key.F9, PrintCommand, "Print current view");
        _hotkeyService.RegisterHotkey(Key.Escape, CancelCommand, "Cancel/Exit current operation");
    }

    /// <summary>
    /// Handles global key presses
    /// </summary>
    /// <param name="key">The key that was pressed</param>
    public bool HandleGlobalKeyPress(Key key)
    {
        return _hotkeyService.HandleKeyPress(key);
    }

    #region Navigation Commands

    [RelayCommand]
    private async Task NavigateToVoucherEntry()
    {
        SelectedTab = "VoucherEntry";
        await _navigationService.NavigateToAsync("VoucherEntry", CurrentCompany);
        StatusMessage = "Voucher Entry";
    }

    [RelayCommand]
    private async Task NavigateToVehicleManagement()
    {
        SelectedTab = "VehicleManagement";
        await _navigationService.NavigateToAsync("VehicleManagement", CurrentCompany);
        StatusMessage = "Vehicle Management";
    }

    [RelayCommand]
    private async Task NavigateToSearch()
    {
        SelectedTab = "Search";
        await _navigationService.NavigateToAsync("Search", CurrentCompany);
        StatusMessage = "Search Vouchers";
    }

    [RelayCommand]
    private async Task NavigateToReports()
    {
        SelectedTab = "Reports";
        await _navigationService.NavigateToAsync("Reports", CurrentCompany);
        StatusMessage = "Reports";
    }


    [RelayCommand]
    private async Task NavigateToUtilities()
    {
        SelectedTab = "Utilities";
        await _navigationService.NavigateToAsync("Utilities", CurrentCompany);
        StatusMessage = "Utilities";
    }

    #endregion

    #region Hotkey Commands

    [RelayCommand]
    private async Task VehicleManagement()
    {
        await NavigateToVehicleManagement();
    }

    [RelayCommand]
    private void AddVoucher()
    {
        // This will be handled by the current active view
        StatusMessage = "Add Voucher (F2)";
    }

    [RelayCommand]
    private async Task Reports()
    {
        await NavigateToReports();
    }

    [RelayCommand]
    private void ProcessWork()
    {
        // Custom processing logic
        StatusMessage = "Process Work (F4)";
    }

    [RelayCommand]
    private void Save()
    {
        // This will be handled by the current active view
        StatusMessage = "Save (F5)";
    }

    [RelayCommand]
    private void Delete()
    {
        // This will be handled by the current active view
        StatusMessage = "Delete (F8)";
    }

    [RelayCommand]
    private void Print()
    {
        // This will be handled by the current active view
        StatusMessage = "Print (F9)";
    }

    [RelayCommand]
    private void Cancel()
    {
        // This will be handled by the current active view or close current operation
        StatusMessage = "Cancel (Esc)";
    }

    #endregion

    #region Company Management

    [RelayCommand]
    private async Task ChangeCompany(Company? company)
    {
        if (company != null && company != CurrentCompany)
        {
            CurrentCompany = company;
            Title = $"Focus Voucher System - {company.Name}";
            
            // Save the selection as default
            await _dataService.Settings.SetValueAsync("DefaultCompanyId", company.CompanyId);
            
            StatusMessage = $"Changed to company: {company.Name}";
            
            // Refresh current view with new company context
            if (!string.IsNullOrEmpty(SelectedTab))
            {
                await _navigationService.NavigateToAsync(SelectedTab, CurrentCompany);
            }
        }
    }

    [RelayCommand]
    private async Task RefreshData()
    {
        await ExecuteAsync(async () =>
        {
            await LoadCompaniesAsync();
            StatusMessage = "Data refreshed";
        }, "Refreshing data...");
    }

    #endregion

    #region INavigationAware Implementation

    public async Task OnNavigatedToAsync(object? parameters)
    {
        await InitializeAsync();
    }

    public async Task OnNavigatedFromAsync()
    {
        // Cleanup if needed
        await Task.CompletedTask;
    }

    #endregion

    #region Additional Commands

    [RelayCommand]
    private void NewCompany()
    {
        StatusMessage = "New Company - Coming Soon";
    }

    [RelayCommand]
    private void ImportData()
    {
        StatusMessage = "Import Data - Coming Soon";
    }

    [RelayCommand]
    private void ExportData()
    {
        StatusMessage = "Export Data - Coming Soon";
    }

    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void ShowHotkeys()
    {
        var hotkeys = _hotkeyService.GetRegisteredHotkeys();
        var message = "Available Hotkeys:\n\n" +
                     string.Join("\n", hotkeys.Select(kv => $"{kv.Key}: {kv.Value}"));
        
        MessageBox.Show(message, "Hotkey Reference", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void About()
    {
        MessageBox.Show(
            "Focus Voucher System\n" +
            "Version 1.0.0\n\n" +
            "A modern replacement for the legacy DOS-based voucher system.\n" +
            "Built with .NET 8, WPF, and SQLite.",
            "About Focus Voucher System", 
            MessageBoxButton.OK, 
            MessageBoxImage.Information);
    }

    #endregion

    public override void Cleanup()
    {
        _hotkeyService.ClearAllHotkeys();
        base.Cleanup();
    }

    [RelayCommand]
    private async Task OpenCompanySelection()
    {
        try
        {
            var vm = new CompanySelectionViewModel(_dataService);
            var window = new CompanySelectionWindow(vm)
            {
                Owner = Application.Current?.MainWindow
            };

            await vm.InitializeAsync();

            var result = window.ShowDialog();
            if (result == true && window.ShouldContinue && window.SelectedCompany != null)
            {
                await ChangeCompany(window.SelectedCompany);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening company selection: {ex.Message}";
        }
    }
}
