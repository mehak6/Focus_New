namespace FocusVoucherSystem.Models;

/// <summary>
/// Represents a vehicle in the recovery statement report
/// </summary>
public class RecoveryItem
{
    /// <summary>
    /// Vehicle number
    /// </summary>
    public string VehicleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Last transaction amount
    /// </summary>
    public decimal LastAmount { get; set; }

    /// <summary>
    /// Last transaction date
    /// </summary>
    public DateTime? LastDate { get; set; }

    /// <summary>
    /// Current vehicle balance
    /// </summary>
    public decimal RemainingBalance { get; set; }

    /// <summary>
    /// Transaction status description (e.g., "No transactions ever", "45 days since last credit", "30 days since last debit")
    /// </summary>
    public string CreditStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether the vehicle has had any transactions (credit or debit)
    /// </summary>
    public bool HasCredits { get; set; }

    /// <summary>
    /// Number of days since last transaction (for sorting purposes)
    /// </summary>
    public int DaysSinceLastCredit { get; set; }

    /// <summary>
    /// Indicates if this is a group header row
    /// </summary>
    public bool IsGroupHeader { get; set; }

    /// <summary>
    /// Vehicle group prefix (e.g., "UP-25", "WB-23")
    /// </summary>
    public string GroupPrefix { get; set; } = string.Empty;
}