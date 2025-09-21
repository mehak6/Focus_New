using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FocusVoucherSystem;

/// <summary>
/// Static class containing commonly used converters
/// </summary>
public static class Converters
{
    public static readonly IValueConverter BooleanToVisibilityConverter = new BooleanToVisibilityConverter();
    public static readonly IValueConverter InverseBooleanToVisibilityConverter = new InverseBooleanToVisibilityConverter();
    public static readonly IValueConverter NullToVisibilityConverter = new NullToVisibilityConverter();
    public static readonly IValueConverter BooleanToActiveStatusConverter = new BooleanToActiveStatusConverter();
    public static readonly IValueConverter VehicleBalanceConverter = new VehicleBalanceConverter();
    public static readonly IValueConverter LastTransactionDateConverter = new LastTransactionDateConverter();
    public static readonly IValueConverter INRCurrencyConverter = new INRCurrencyConverter();
    public static readonly IValueConverter DecimalToBalanceTypeConverter = new DecimalToBalanceTypeConverter();
    public static readonly IValueConverter InverseBooleanConverter = new InverseBooleanConverter();
}

/// <summary>
/// Converts boolean to Visibility (true = Visible, false = Collapsed)
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility == Visibility.Visible;
        
        return false;
    }
}

/// <summary>
/// Converts boolean to Visibility (true = Collapsed, false = Visible)
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility == Visibility.Collapsed;
        
        return true;
    }
}

/// <summary>
/// Converts null to Visibility (null = Collapsed, not null = Visible)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to Active/Inactive status text
/// </summary>
public class BooleanToActiveStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? "Active" : "Inactive";
        
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for vehicle balance with INR formatting
/// </summary>
public class VehicleBalanceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            return FormatAsINR(amount);
        }
        
        return "₹0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
    
    private string FormatAsINR(decimal amount)
    {
        return $"₹{amount:N2}";
    }
}

/// <summary>
/// Converter for last transaction date (placeholder - will be enhanced)
/// </summary>
public class LastTransactionDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // This is a placeholder - in a real implementation, you'd get the actual last transaction date
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for displaying amounts in Indian Rupee (INR) format
/// </summary>
public class INRCurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return "₹0.00";
        
        if (value is decimal amount)
        {
            return FormatAsINR(amount);
        }
        
        if (value is double doubleAmount)
        {
            return FormatAsINR((decimal)doubleAmount);
        }
        
        if (value is float floatAmount)
        {
            return FormatAsINR((decimal)floatAmount);
        }
        
        if (decimal.TryParse(value.ToString(), out decimal parsedAmount))
        {
            return FormatAsINR(parsedAmount);
        }
        
        return "₹0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue && !string.IsNullOrWhiteSpace(strValue))
        {
            // Remove currency symbol and any formatting
            var cleanValue = strValue.Replace("₹", "").Replace(",", "").Trim();
            if (decimal.TryParse(cleanValue, out decimal amount))
            {
                return amount;
            }
        }
        return 0m;
    }
    
    private string FormatAsINR(decimal amount)
    {
        // Format with Indian numbering system (lakhs, crores)
        var formattedAmount = amount.ToString("N2", new CultureInfo("en-IN"));
        return $"₹{formattedAmount}";
    }
}

/// <summary>
/// Converter for determining balance type (Dr/Cr) from decimal value
/// </summary>
public class DecimalToBalanceTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            return amount >= 0 ? "D" : "C";
        }

        return "D"; // Default to Debit
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to inverse boolean (true = false, false = true)
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }
}
