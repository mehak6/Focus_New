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
    /// Credit status description (e.g., "No credits ever", "45 days since last credit")
    /// </summary>
    public string CreditStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether the vehicle has had any credit transactions
    /// </summary>
    public bool HasCredits { get; set; }

    /// <summary>
    /// Number of days since last credit (for sorting purposes)
    /// </summary>
    public int DaysSinceLastCredit { get; set; }
}