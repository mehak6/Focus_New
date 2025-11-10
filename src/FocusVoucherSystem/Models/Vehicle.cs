using System.ComponentModel.DataAnnotations;

namespace FocusVoucherSystem.Models;

/// <summary>
/// Represents a vehicle/account entity in the voucher system
/// </summary>
public class Vehicle
{
    /// <summary>
    /// Unique identifier for the vehicle
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Company that owns this vehicle
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// Vehicle number/identifier (e.g., license plate, account code)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string VehicleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the vehicle
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the vehicle is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the record was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// When the record was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Navigation property to the company that owns this vehicle
    /// </summary>
    public virtual Company? Company { get; set; }

    /// <summary>
    /// Navigation property to vouchers for this vehicle
    /// </summary>
    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();

    /// <summary>
    /// Gets the display name for this vehicle (includes description if available)
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Description) 
        ? VehicleNumber 
        : $"{VehicleNumber} - {Description}";

    /// <summary>
    /// Gets the formatted balance for this vehicle in INR format
    /// </summary>
    public string FormattedBalance => $"₹{CalculateBalance().ToString("N2", System.Globalization.CultureInfo.CreateSpecificCulture("en-IN"))}";

    /// <summary>
    /// Calculates the current balance for this vehicle
    /// Note: This should be computed via repository for performance
    /// </summary>
    public decimal CalculateBalance()
    {
        return Vouchers
            .Where(v => v.VehicleId == VehicleId)
            .Sum(v => v.DrCr == "D" ? v.Amount : -v.Amount);
    }

    /// <summary>
    /// Gets the last transaction date for this vehicle
    /// </summary>
    public DateTime? GetLastTransactionDate()
    {
        return Vouchers
            .Where(v => v.VehicleId == VehicleId)
            .OrderByDescending(v => v.Date)
            .FirstOrDefault()?.Date;
    }

    /// <summary>
    /// Checks if this vehicle matches the search term
    /// </summary>
    /// <param name="searchTerm">Term to search for</param>
    /// <returns>True if the vehicle matches the search term</returns>
    public bool MatchesSearch(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;

        var term = searchTerm.ToLowerInvariant();
        
        return VehicleNumber.ToLowerInvariant().Contains(term) ||
               (Description?.ToLowerInvariant().Contains(term) ?? false);
    }

    public override string ToString()
    {
        return DisplayName;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vehicle vehicle && VehicleId == vehicle.VehicleId;
    }

    public override int GetHashCode()
    {
        return VehicleId.GetHashCode();
    }
}