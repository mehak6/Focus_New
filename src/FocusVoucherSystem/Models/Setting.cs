using System.ComponentModel.DataAnnotations;

namespace FocusVoucherSystem.Models;

/// <summary>
/// Represents a configuration setting in the system
/// </summary>
public class Setting
{
    /// <summary>
    /// Setting key (unique identifier)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this setting controls
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// When the setting was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets the value as a boolean
    /// </summary>
    /// <param name="defaultValue">Default value if parsing fails</param>
    /// <returns>Boolean value</returns>
    public bool GetBooleanValue(bool defaultValue = false)
    {
        if (bool.TryParse(Value, out bool result))
            return result;
        
        // Handle common string representations
        return Value.ToLowerInvariant() switch
        {
            "yes" => true,
            "no" => false,
            "1" => true,
            "0" => false,
            "on" => true,
            "off" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Gets the value as an integer
    /// </summary>
    /// <param name="defaultValue">Default value if parsing fails</param>
    /// <returns>Integer value</returns>
    public int GetIntValue(int defaultValue = 0)
    {
        return int.TryParse(Value, out int result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets the value as a decimal
    /// </summary>
    /// <param name="defaultValue">Default value if parsing fails</param>
    /// <returns>Decimal value</returns>
    public decimal GetDecimalValue(decimal defaultValue = 0m)
    {
        return decimal.TryParse(Value, out decimal result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets the value as a DateTime
    /// </summary>
    /// <param name="defaultValue">Default value if parsing fails</param>
    /// <returns>DateTime value</returns>
    public DateTime GetDateTimeValue(DateTime defaultValue = default)
    {
        return DateTime.TryParse(Value, out DateTime result) ? result : defaultValue;
    }

    /// <summary>
    /// Sets the value from a boolean
    /// </summary>
    /// <param name="value">Boolean value to set</param>
    public void SetValue(bool value)
    {
        Value = value.ToString().ToLowerInvariant();
        ModifiedDate = DateTime.Now;
    }

    /// <summary>
    /// Sets the value from an integer
    /// </summary>
    /// <param name="value">Integer value to set</param>
    public void SetValue(int value)
    {
        Value = value.ToString();
        ModifiedDate = DateTime.Now;
    }

    /// <summary>
    /// Sets the value from a decimal
    /// </summary>
    /// <param name="value">Decimal value to set</param>
    public void SetValue(decimal value)
    {
        Value = value.ToString();
        ModifiedDate = DateTime.Now;
    }

    /// <summary>
    /// Sets the value from a DateTime
    /// </summary>
    /// <param name="value">DateTime value to set</param>
    public void SetValue(DateTime value)
    {
        Value = value.ToString("yyyy-MM-dd HH:mm:ss");
        ModifiedDate = DateTime.Now;
    }

    /// <summary>
    /// Sets the value from a string
    /// </summary>
    /// <param name="value">String value to set</param>
    public void SetValue(string value)
    {
        Value = value ?? string.Empty;
        ModifiedDate = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Key} = {Value}";
    }

    public override bool Equals(object? obj)
    {
        return obj is Setting setting && Key == setting.Key;
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}