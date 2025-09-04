using FocusVoucherSystem.Services;
using FocusVoucherSystem.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Globalization;

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

    [RelayCommand]
    private async Task BackupCsv()
    {
        if (SelectedCompany == null)
        {
            StatusMessage = "Select a company first.";
            return;
        }

        // Ask user to choose a destination folder via SaveFileDialog (we'll use the folder part)
        var dlg = new SaveFileDialog
        {
            Title = "Select destination folder for CSV backup",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"{SanitizeFileName(SelectedCompany.Name)}_backup_placeholder.csv",
            OverwritePrompt = false
        };

        if (dlg.ShowDialog() != true)
            return;

        var destFolder = Path.GetDirectoryName(dlg.FileName);
        if (string.IsNullOrWhiteSpace(destFolder))
        {
            StatusMessage = "Invalid destination folder.";
            return;
        }

        SetBusy(true, "Exporting CSV backup...");
        try
        {
            // Fetch data
            var vehicles = await _dataService.Vehicles.GetByCompanyIdAsync(SelectedCompany.CompanyId);
            // Use wide date range to include all vouchers
            var vouchers = await _dataService.Vouchers.GetByDateRangeAsync(
                SelectedCompany.CompanyId,
                new DateTime(1900, 1, 1),
                new DateTime(2100, 1, 1));

            // Prepare filenames
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var companySlug = SanitizeFileName(SelectedCompany.Name);
            var vehiclesCsvPath = Path.Combine(destFolder, $"{companySlug}_Vehicles_{timestamp}.csv");
            var vouchersCsvPath = Path.Combine(destFolder, $"{companySlug}_Vouchers_{timestamp}.csv");

            // Write Vehicles CSV
            using (var sw = new StreamWriter(vehiclesCsvPath))
            {
                sw.WriteLine("VehicleId,VehicleNumber,Description,IsActive,CreatedDate,ModifiedDate");
                foreach (var v in vehicles)
                {
                    sw.WriteLine(string.Join(',', new[]
                    {
                        v.VehicleId.ToString(CultureInfo.InvariantCulture),
                        Csv(v.VehicleNumber),
                        Csv(v.Description ?? string.Empty),
                        v.IsActive ? "1" : "0",
                        v.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        v.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                    }));
                }
            }

            // Write Vouchers CSV
            using (var sw = new StreamWriter(vouchersCsvPath))
            {
                sw.WriteLine("VoucherId,VoucherNumber,Date,VehicleId,VehicleNumber,Amount,DrCr,Narration");
                foreach (var v in vouchers)
                {
                    var vehicleNumber = v.Vehicle?.VehicleNumber ?? string.Empty;
                    sw.WriteLine(string.Join(',', new[]
                    {
                        v.VoucherId.ToString(CultureInfo.InvariantCulture),
                        v.VoucherNumber.ToString(CultureInfo.InvariantCulture),
                        v.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        v.VehicleId.ToString(CultureInfo.InvariantCulture),
                        Csv(vehicleNumber),
                        v.Amount.ToString(CultureInfo.InvariantCulture),
                        Csv(v.DrCr),
                        Csv(v.Narration ?? string.Empty)
                    }));
                }
            }

            StatusMessage = $"Backup created: {Path.GetFileName(vehiclesCsvPath)}, {Path.GetFileName(vouchersCsvPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static string Csv(string value)
    {
        if (value == null) return string.Empty;
        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (needsQuotes)
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }
        return value;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }
}
