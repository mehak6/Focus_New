using System.Windows;
using System.Windows.Input;
using FocusVoucherSystem.ViewModels;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Views;
using FocusVoucherSystem.Models;

namespace FocusVoucherSystem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly NavigationService _navigationService;
    private readonly HotkeyService _hotkeyService;
    private readonly DataService _dataService;

    public MainWindow(DataService dataService, Company selectedCompany)
    {
        System.Diagnostics.Debug.WriteLine($"MainWindow constructor: Started with company {selectedCompany?.Name}");
        InitializeComponent();

        // Use provided services
        _dataService = dataService;
        _navigationService = new NavigationService();
        _hotkeyService = new HotkeyService();

        // Create and set ViewModel
        _viewModel = new MainWindowViewModel(_dataService, _navigationService, _hotkeyService);
        DataContext = _viewModel;

        // Set the current company
        _viewModel.CurrentCompany = selectedCompany;
        _viewModel.Title = $"Focus Voucher System - {selectedCompany.Name}";

        System.Diagnostics.Debug.WriteLine($"MainWindow constructor: Set company to {selectedCompany.Name}");

        // Set up navigation host
        _navigationService.SetNavigationHost(ContentHost);
        _navigationService.SetDataService(_dataService);

        // Register views
        RegisterViews();

        System.Diagnostics.Debug.WriteLine($"MainWindow constructor: Completed initialization");

        // Initialize the application (simplified since company is already selected)
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: Started");
        try
        {
            // Company is already selected, just initialize UI components
            _viewModel.StatusMessage = "Application ready";

            System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: About to navigate to VoucherEntry");

            // Navigate to default view (Voucher Entry) on startup
            await _navigationService.NavigateToAsync("VoucherEntry", _viewModel.CurrentCompany);

            System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: Navigation completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: ERROR - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            MessageBox.Show($"Failed to initialize application: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        try
        {
            _viewModel?.Cleanup();
            // Note: DataService disposal is handled by App.xaml.cs
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Handles global keyboard shortcuts
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Let the ViewModel handle the key press through the hotkey service
        var handled = _viewModel.HandleGlobalKeyPress(e.Key);
        
        if (handled)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Registers all views with the navigation service
    /// </summary>
        private void RegisterViews()
        {
            _navigationService.RegisterView("VoucherEntry", typeof(VoucherEntryView), typeof(VoucherEntryViewModel));
            _navigationService.RegisterView("VehicleManagement", typeof(VehicleManagementView), typeof(VehicleManagementViewModel));
            _navigationService.RegisterView("Search", typeof(SearchView), typeof(SearchViewModel));
            _navigationService.RegisterView("Reports", typeof(ReportsView), typeof(ReportsViewModel));
            _navigationService.RegisterView("Utilities", typeof(UtilitiesView), typeof(UtilitiesViewModel));
            // Add more views as they are created
        }


}
