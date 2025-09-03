using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;

namespace FocusVoucherSystem.ViewModels;

public partial class UtilitiesViewModel : BaseViewModel, INavigationAware
{
    private readonly ImportService _importService;
    private readonly NavigationService _navigationService;

    public UtilitiesViewModel(DataService dataService, NavigationService navigationService) : base(dataService)
    {
        _importService = new ImportService(dataService);
        _navigationService = navigationService;
    }

    public async Task OnNavigatedToAsync(object? parameters)
    {
        await LoadCompaniesAsync();
    }

    public async Task OnNavigatedFromAsync()
    {
        await Task.CompletedTask;
    }

    [ObservableProperty]
    private ObservableCollection<Company> _companies = new();

    [ObservableProperty]
    private Company? _selectedCompany;

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _log = string.Empty;

    [ObservableProperty]
    private bool _createMissingVehicles = true;

    [ObservableProperty]
    private bool _dryRun = true;

    [ObservableProperty]
    private ObservableCollection<string> _previewVehicles = new();

    public partial class PreviewVoucher
    {
        public int VNo { get; set; }
        public DateTime Date { get; set; }
        public string Vehicle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string DrCr { get; set; } = "D";
    }

    [ObservableProperty]
    private ObservableCollection<PreviewVoucher> _previewVouchers = new();

    private async Task LoadCompaniesAsync()
    {
        Companies.Clear();
        var list = await _dataService.Companies.GetActiveCompaniesAsync();
        foreach (var c in list) Companies.Add(c);
        if (Companies.Count > 0) SelectedCompany = Companies[0];
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select VEH.TXT or VCH.TXT",
            Filter = "Legacy files|VEH.TXT;VCH.TXT|All files|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            var dir = Path.GetDirectoryName(dlg.FileName);
            if (!string.IsNullOrEmpty(dir)) FolderPath = dir;
            _ = LoadPreviewAsync();
        }
    }

    [RelayCommand]
    private async Task DryRunImport()
    {
        if (SelectedCompany == null || string.IsNullOrWhiteSpace(FolderPath))
        {
            StatusMessage = "Select company and folder first.";
            return;
        }
        SetBusy(true, "Running dry run...");
        try
        {
            var res = await _importService.ImportAsync(new ImportOptions
            {
                FolderPath = FolderPath,
                CompanyId = SelectedCompany.CompanyId,
                DryRun = true,
                CreateMissingVehicles = CreateMissingVehicles
            });
            Log = res.ToString();
            StatusMessage = "Dry run completed";
        }
        catch (Exception ex)
        {
            Log = ex.Message;
            StatusMessage = "Dry run failed";
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task RunImport()
    {
        if (SelectedCompany == null || string.IsNullOrWhiteSpace(FolderPath))
        {
            StatusMessage = "Select company and folder first.";
            return;
        }
        SetBusy(true, "Importing data...");
        try
        {
            var res = await _importService.ImportAsync(new ImportOptions
            {
                FolderPath = FolderPath,
                CompanyId = SelectedCompany.CompanyId,
                DryRun = false,
                CreateMissingVehicles = CreateMissingVehicles
            });
            Log = res.ToString();
            StatusMessage = "Import completed";
            // After import, navigate to Voucher Entry to refresh the list and next voucher number
            await _navigationService.NavigateToAsync("VoucherEntry", SelectedCompany);
        }
        catch (Exception ex)
        {
            Log = ex.Message;
            StatusMessage = "Import failed";
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task ClearDatabase()
    {
        if (SelectedCompany == null)
        {
            StatusMessage = "Select a company first.";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"This will delete all vouchers and vehicles for {SelectedCompany.Name} and reset numbering. Continue?",
            "Confirm Clear",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        SetBusy(true, "Clearing data...");
        try
        {
            await _dataService.ClearCompanyDataAsync(SelectedCompany.CompanyId);
            StatusMessage = "Company data cleared";
            await LoadPreviewAsync();
            await _navigationService.NavigateToAsync("VoucherEntry", SelectedCompany);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Clear failed: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task LoadPreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(FolderPath)) return;
        try
        {
            SetBusy(true, "Loading preview...");
            PreviewVehicles.Clear();
            PreviewVouchers.Clear();

            var vehFile = Path.Combine(FolderPath, "VEH.TXT");
            var vchFile = Path.Combine(FolderPath, "VCH.TXT");

            if (File.Exists(vehFile))
            {
                var vehicles = await _importService.ParseVehiclesAsync(vehFile);
                foreach (var v in vehicles) PreviewVehicles.Add(v);
            }

            if (File.Exists(vchFile))
            {
                var vouchers = await _importService.ParseVouchersAsync(vchFile);
                foreach (var v in vouchers)
                {
                    PreviewVouchers.Add(new PreviewVoucher
                    {
                        VNo = v.VNo,
                        Date = v.Date,
                        Vehicle = v.Vehicle,
                        Amount = v.Amount,
                        DrCr = v.DrCr
                    });
                }
            }
            StatusMessage = $"Preview loaded: {PreviewVehicles.Count} vehicles, {PreviewVouchers.Count} vouchers.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Preview failed: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    partial void OnFolderPathChanged(string value)
    {
        _ = LoadPreviewAsync();
    }
}
