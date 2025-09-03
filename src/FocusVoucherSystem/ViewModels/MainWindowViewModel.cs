using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
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
    /// Initializes the main window
    /// </summary>
    public async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Initialize database
            await _dataService.InitializeDatabaseAsync();
            
            // Load companies
            await LoadCompaniesAsync();
            
            // Set default company
            await SetDefaultCompanyAsync();
            
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
        _hotkeyService.RegisterHotkey(Key.F3, ReportsCommand, "Reports menu");
        _hotkeyService.RegisterHotkey(Key.F4, ProcessWorkCommand, "Process Work");
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
}