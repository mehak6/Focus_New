using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FocusVoucherSystem.Models;

/// <summary>
/// Represents a voucher entry in the system
/// </summary>
public partial class Voucher : ObservableObject
{
    /// <summary>
    /// Unique identifier for the voucher
    /// </summary>
    public int VoucherId { get; set; }

    /// <summary>
    /// Company this voucher belongs to
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// User-visible voucher number (editable, continuous per company)
    /// </summary>
    [ObservableProperty]
    private int _voucherNumber;

    /// <summary>
    /// Date of the voucher entry
    /// </summary>
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Vehicle/Account this voucher is for
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Amount of the transaction (always positive)
    /// </summary>
    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Debit or Credit indicator ('D' or 'C')
    /// </summary>
    [Required]
    [StringLength(1)]
    [RegularExpression("^[DC]$", ErrorMessage = "DrCr must be either 'D' or 'C'")]
    public string DrCr { get; set; } = "D";

    /// <summary>
    /// Optional narration/description of the transaction
    /// </summary>
    [StringLength(500)]
    public string? Narration { get; set; }

    /// <summary>
    /// When the record was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// When the record was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Navigation property to the company
    /// </summary>
    public virtual Company? Company { get; set; }

    /// <summary>
    /// Navigation property to the vehicle/account
    /// </summary>
    public virtual Vehicle? Vehicle { get; set; }

    /// <summary>
    /// Running balance after this transaction (calculated for display)
    /// </summary>
    [ObservableProperty]
    private decimal _runningBalance;

    /// <summary>
    /// Gets whether this is a debit entry
    /// </summary>
    public bool IsDebit => DrCr == "D";

    /// <summary>
    /// Gets whether this is a credit entry
    /// </summary>
    public bool IsCredit => DrCr == "C";

    /// <summary>
    /// Gets the signed amount (positive for debit, negative for credit)
    /// </summary>
    public decimal SignedAmount => IsDebit ? Amount : -Amount;

    /// <summary>
    /// Gets a formatted display string for the amount with Dr/Cr indicator
    /// </summary>
    public string FormattedAmount => $"{Amount:N2} {DrCr}";

    /// <summary>
    /// Gets a short description for display purposes
    /// </summary>
    public string DisplayDescription => string.IsNullOrWhiteSpace(Narration) 
        ? $"{Vehicle?.VehicleNumber} - {FormattedAmount}"
        : $"{Vehicle?.VehicleNumber} - {Narration}";

    /// <summary>
    /// Validates the voucher data
    /// </summary>
    /// <returns>List of validation errors (empty if valid)</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (VoucherNumber <= 0)
            errors.Add("Voucher number must be greater than 0");

        if (Amount <= 0)
            errors.Add("Amount must be greater than 0");

        if (DrCr != "D" && DrCr != "C")
            errors.Add("DrCr must be either 'D' or 'C'");

        if (VehicleId <= 0)
            errors.Add("Vehicle must be selected");

        if (CompanyId <= 0)
            errors.Add("Company must be selected");

        if (Date == default)
            errors.Add("Date must be specified");

        return errors;
    }

    /// <summary>
    /// Creates a copy of this voucher with a new voucher number
    /// </summary>
    /// <param name="newVoucherNumber">New voucher number to assign</param>
    /// <returns>Copy of the voucher with new number</returns>
    public Voucher Clone(int newVoucherNumber)
    {
        return new Voucher
        {
            CompanyId = CompanyId,
            VoucherNumber = newVoucherNumber,
            Date = Date,
            VehicleId = VehicleId,
            Amount = Amount,
            DrCr = DrCr,
            Narration = Narration
        };
    }

    public override string ToString()
    {
        return $"V{VoucherNumber:000} - {Date:dd/MM/yyyy} - {FormattedAmount}";
    }

    public override bool Equals(object? obj)
    {
        return obj is Voucher voucher && VoucherId == voucher.VoucherId;
    }

    public override int GetHashCode()
    {
        return VoucherId.GetHashCode();
    }
}
