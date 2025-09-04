using CommunityToolkit.Mvvm.ComponentModel;

namespace FocusVoucherSystem.Models;

/// <summary>
/// Display wrapper for Vehicle with computed balance and transaction data
/// Used in UI to show calculated fields without modifying the core Vehicle entity
/// </summary>
public partial class VehicleDisplayItem : ObservableObject
{
    private readonly Vehicle _vehicle;

    public VehicleDisplayItem(Vehicle vehicle)
    {
        _vehicle = vehicle;
    }

    /// <summary>
    /// The underlying vehicle entity
    /// </summary>
    public Vehicle Vehicle => _vehicle;

    /// <summary>
    /// Vehicle ID
    /// </summary>
    public int VehicleId => _vehicle.VehicleId;

    /// <summary>
    /// Company ID
    /// </summary>
    public int CompanyId => _vehicle.CompanyId;

    /// <summary>
    /// Vehicle number
    /// </summary>
    public string VehicleNumber => _vehicle.VehicleNumber;

    /// <summary>
    /// Vehicle description
    /// </summary>
    public string? Description => _vehicle.Description;

    /// <summary>
    /// Is vehicle active
    /// </summary>
    public bool IsActive => _vehicle.IsActive;

    /// <summary>
    /// Display name for the vehicle
    /// </summary>
    public string DisplayName => _vehicle.DisplayName;

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedDate => _vehicle.CreatedDate;

    /// <summary>
    /// Modified date
    /// </summary>
    public DateTime ModifiedDate => _vehicle.ModifiedDate;

    /// <summary>
    /// Current balance for this vehicle
    /// </summary>
    [ObservableProperty]
    private decimal _balance;

    /// <summary>
    /// Formatted balance string
    /// </summary>
    public string FormattedBalance => Balance.ToString("C2");

    /// <summary>
    /// Last transaction date for this vehicle
    /// </summary>
    [ObservableProperty]
    private DateTime? _lastTransactionDate;

    /// <summary>
    /// Formatted last transaction date
    /// </summary>
    public string FormattedLastTransactionDate => 
        LastTransactionDate?.ToString("dd/MM/yyyy") ?? "-";

    /// <summary>
    /// Updates the balance value
    /// </summary>
    /// <param name="balance">New balance value</param>
    public void UpdateBalance(decimal balance)
    {
        Balance = balance;
        OnPropertyChanged(nameof(FormattedBalance));
    }

    /// <summary>
    /// Updates the last transaction date
    /// </summary>
    /// <param name="date">Last transaction date</param>
    public void UpdateLastTransactionDate(DateTime? date)
    {
        LastTransactionDate = date;
        OnPropertyChanged(nameof(FormattedLastTransactionDate));
    }

    /// <summary>
    /// Creates a copy of the underlying vehicle for editing
    /// </summary>
    /// <returns>Copy of the vehicle</returns>
    public Vehicle CreateVehicleCopy()
    {
        return new Vehicle
        {
            VehicleId = _vehicle.VehicleId,
            CompanyId = _vehicle.CompanyId,
            VehicleNumber = _vehicle.VehicleNumber,
            Description = _vehicle.Description,
            IsActive = _vehicle.IsActive,
            CreatedDate = _vehicle.CreatedDate,
            ModifiedDate = _vehicle.ModifiedDate,
            Company = _vehicle.Company
        };
    }

    /// <summary>
    /// Updates the underlying vehicle with new data
    /// </summary>
    /// <param name="updatedVehicle">Updated vehicle data</param>
    public void UpdateVehicleData(Vehicle updatedVehicle)
    {
        _vehicle.VehicleId = updatedVehicle.VehicleId;
        _vehicle.CompanyId = updatedVehicle.CompanyId;
        _vehicle.VehicleNumber = updatedVehicle.VehicleNumber;
        _vehicle.Description = updatedVehicle.Description;
        _vehicle.IsActive = updatedVehicle.IsActive;
        _vehicle.CreatedDate = updatedVehicle.CreatedDate;
        _vehicle.ModifiedDate = updatedVehicle.ModifiedDate;
        _vehicle.Company = updatedVehicle.Company;

        // Notify of property changes
        OnPropertyChanged(nameof(VehicleNumber));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(ModifiedDate));
    }
}
