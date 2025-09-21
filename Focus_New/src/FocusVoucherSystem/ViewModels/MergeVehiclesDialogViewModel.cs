using CommunityToolkit.Mvvm.ComponentModel;
using FocusVoucherSystem.Models;
using FocusVoucherSystem.Services;
using System.Collections.ObjectModel;
using Dapper;

namespace FocusVoucherSystem.ViewModels;

/// <summary>
/// Simple wrapper for ComboBox display to avoid binding issues
/// </summary>
public class VehicleComboBoxItem
{
    public VehicleDisplayItem Vehicle { get; set; }
    public string DisplayText { get; set; }

    public VehicleComboBoxItem(VehicleDisplayItem vehicle)
    {
        Vehicle = vehicle;
        DisplayText = $"{vehicle.VehicleNumber} - {vehicle.Description}";
    }

    public override string ToString() => DisplayText;
}

public partial class MergeVehiclesDialogViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<VehicleComboBoxItem> _availableVehicles = new();

    [ObservableProperty]
    private VehicleComboBoxItem? _sourceVehicle;

    [ObservableProperty]
    private VehicleComboBoxItem? _targetVehicle;

    [ObservableProperty]
    private ObservableCollection<VehicleComboBoxItem> _targetVehicles = new();

    [ObservableProperty]
    private bool _showSummary;

    [ObservableProperty]
    private bool _canMerge;

    [ObservableProperty]
    private int _sourceVehicleVoucherCount;

    [ObservableProperty]
    private int _targetVehicleVoucherCount;

    private readonly Company _currentCompany;

    public MergeVehiclesDialogViewModel(DataService dataService, Company currentCompany,
        IEnumerable<VehicleDisplayItem> vehicles) : base(dataService)
    {
        _currentCompany = currentCompany ?? throw new ArgumentNullException(nameof(currentCompany));

        try
        {
            var activeVehicles = vehicles?.Where(v => v?.IsActive == true) ?? Enumerable.Empty<VehicleDisplayItem>();
            foreach (var vehicle in activeVehicles)
            {
                var comboItem = new VehicleComboBoxItem(vehicle);
                AvailableVehicles.Add(comboItem);
            }

            // Initialize target vehicles collection
            foreach (var vehicle in AvailableVehicles)
            {
                TargetVehicles.Add(vehicle);
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to initialize merge dialog: {ex.Message}");
        }
    }

    partial void OnSourceVehicleChanged(VehicleComboBoxItem? value)
    {
        UpdateTargetVehicles();
        _ = UpdateSummaryAsync();
    }

    partial void OnTargetVehicleChanged(VehicleComboBoxItem? value)
    {
        _ = UpdateSummaryAsync();
    }

    private void UpdateTargetVehicles()
    {
        TargetVehicles.Clear();

        if (SourceVehicle == null)
        {
            foreach (var vehicle in AvailableVehicles)
            {
                TargetVehicles.Add(vehicle);
            }
        }
        else
        {
            foreach (var vehicle in AvailableVehicles.Where(v => v.Vehicle.VehicleId != SourceVehicle.Vehicle.VehicleId))
            {
                TargetVehicles.Add(vehicle);
            }
        }

        if (TargetVehicle != null && !TargetVehicles.Contains(TargetVehicle))
        {
            TargetVehicle = null;
        }
    }

    private async Task UpdateSummaryAsync()
    {
        ShowSummary = SourceVehicle != null && TargetVehicle != null;
        CanMerge = ShowSummary && SourceVehicle?.Vehicle.VehicleId != TargetVehicle?.Vehicle.VehicleId;

        if (ShowSummary && SourceVehicle != null && TargetVehicle != null)
        {
            try
            {
                SourceVehicleVoucherCount = await GetVoucherCountAsync(SourceVehicle.Vehicle.VehicleId);
                TargetVehicleVoucherCount = await GetVoucherCountAsync(TargetVehicle.Vehicle.VehicleId);
            }
            catch
            {
                SourceVehicleVoucherCount = 0;
                TargetVehicleVoucherCount = 0;
            }
        }
        else
        {
            SourceVehicleVoucherCount = 0;
            TargetVehicleVoucherCount = 0;
        }
    }

    private async Task<int> GetVoucherCountAsync(int vehicleId)
    {
        var connection = await _dataService.GetConnectionAsync();
        const string sql = "SELECT COUNT(*) FROM Vouchers WHERE VehicleId = @VehicleId";
        return await connection.QuerySingleAsync<int>(sql, new { VehicleId = vehicleId });
    }

    public async Task<bool> MergeVehiclesAsync()
    {
        if (SourceVehicle == null || TargetVehicle == null)
            return false;

        try
        {
            return await _dataService.Vehicles.MergeVehiclesAsync(SourceVehicle.Vehicle.VehicleId, TargetVehicle.Vehicle.VehicleId);
        }
        catch
        {
            return false;
        }
    }
}