using System.Configuration;
using System.Data;
using System.Windows;
using FocusVoucherSystem.Services;
using FocusVoucherSystem.Views;
using FocusVoucherSystem.ViewModels;
using FocusVoucherSystem.Models;

namespace FocusVoucherSystem;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"App.xaml.cs: Application_Startup started");

            // Prevent the app from auto-shutting down when the selection dialog closes
            // Default is OnLastWindowClose, which can terminate the app before MainWindow is shown
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Initialize services
            var dataService = new DataService();

            // Create and show company selection window
            var companySelectionViewModel = new CompanySelectionViewModel(dataService);
            var companySelectionWindow = new CompanySelectionWindow(companySelectionViewModel);

            System.Diagnostics.Debug.WriteLine($"App.xaml.cs: CompanySelectionWindow created");

            // Initialize the company selection view model
            await companySelectionViewModel.InitializeAsync();

            System.Diagnostics.Debug.WriteLine($"App.xaml.cs: ViewModel initialized");

            // Show the company selection window as a modal dialog
            var result = companySelectionWindow.ShowDialog();

            System.Diagnostics.Debug.WriteLine($"App.xaml.cs: ShowDialog() returned: {result}");
            System.Diagnostics.Debug.WriteLine($"App.xaml.cs: ShouldContinue: {companySelectionWindow.ShouldContinue}");
            System.Diagnostics.Debug.WriteLine($"App.xaml.cs: SelectedCompany: {companySelectionWindow.SelectedCompany?.Name} (ID: {companySelectionWindow.SelectedCompany?.CompanyId})");

            if (result == true && companySelectionWindow.ShouldContinue && companySelectionWindow.SelectedCompany != null)
            {
                System.Diagnostics.Debug.WriteLine($"App.xaml.cs: SUCCESS - continuing to MainWindow");

                // User selected a company, continue to main application
                var selectedCompany = companySelectionWindow.SelectedCompany;

                try
                {
                    // Create and show main window with selected company
                    var mainWindow = new MainWindow(dataService, selectedCompany);
                    MainWindow = mainWindow;
                    mainWindow.Show();

                    // Restore normal shutdown behavior now that MainWindow is shown
                    ShutdownMode = ShutdownMode.OnMainWindowClose;

                    System.Diagnostics.Debug.WriteLine($"App.xaml.cs: MainWindow created and shown successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"App.xaml.cs: ERROR creating/showing MainWindow: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    MessageBox.Show($"Failed to start main application: {ex.Message}", "Startup Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"App.xaml.cs: Cancellation conditions met - exiting application");
                // User cancelled or no company selected, exit application
                dataService.Dispose();
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to start application:\n\n{ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
            }
            errorMessage += $"\n\nStack Trace:\n{ex.StackTrace}";
            
            MessageBox.Show(errorMessage, "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
