using System.ComponentModel.DataAnnotations;

namespace FocusVoucherSystem.Models;

/// <summary>
/// Represents a company entity in the voucher system
/// </summary>
public class Company
{
    /// <summary>
    /// Unique identifier for the company
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// Company name (must be unique)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Financial year start date
    /// </summary>
    public DateTime FinancialYearStart { get; set; } = new(DateTime.Now.Year, 4, 1); // Apr 1st

    /// <summary>
    /// Financial year end date  
    /// </summary>
    public DateTime FinancialYearEnd { get; set; } = new(DateTime.Now.Year + 1, 3, 31); // Mar 31st

    /// <summary>
    /// Last voucher number used for this company (for continuous numbering)
    /// </summary>
    public int LastVoucherNumber { get; set; }

    /// <summary>
    /// Whether the company is active
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
    /// Navigation property to vehicles owned by this company
    /// </summary>
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    /// <summary>
    /// Navigation property to vouchers for this company
    /// </summary>
    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();

    /// <summary>
    /// Gets the next available voucher number for this company
    /// </summary>
    public int GetNextVoucherNumber()
    {
        return LastVoucherNumber + 1;
    }

    /// <summary>
    /// Updates the last voucher number used
    /// </summary>
    /// <param name="voucherNumber">The voucher number that was just used</param>
    public void UpdateLastVoucherNumber(int voucherNumber)
    {
        if (voucherNumber > LastVoucherNumber)
        {
            LastVoucherNumber = voucherNumber;
        }
    }

    /// <summary>
    /// Checks if the given date falls within the company's financial year
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <returns>True if the date is within the financial year</returns>
    public bool IsDateInFinancialYear(DateTime date)
    {
        return date >= FinancialYearStart && date <= FinancialYearEnd;
    }

    /// <summary>
    /// Gets a formatted display string for the financial year
    /// </summary>
    public string FinancialYearDisplay => $"FY: {FinancialYearStart:dd/MM/yyyy} - {FinancialYearEnd:dd/MM/yyyy}";

    public override string ToString()
    {
        return Name;
    }
}