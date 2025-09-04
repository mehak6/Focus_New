using System.Windows;
using System.Windows.Input;
using FocusVoucherSystem.ViewModels;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Views;

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

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize services
        _dataService = new DataService();
        _navigationService = new NavigationService();
        _hotkeyService = new HotkeyService();
        
        // Create and set ViewModel
        _viewModel = new MainWindowViewModel(_dataService, _navigationService, _hotkeyService);
        DataContext = _viewModel;
        
        // Set up navigation host
        _navigationService.SetNavigationHost(ContentHost);
        _navigationService.SetDataService(_dataService);
        
        // Register views
        RegisterViews();
        
        // Initialize the application
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
            // Navigate to default view (Voucher Entry) on startup
            await _navigationService.NavigateToAsync("VoucherEntry", _viewModel.CurrentCompany);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize application: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        try
        {
            _viewModel?.Cleanup();
            _dataService?.Dispose();
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
