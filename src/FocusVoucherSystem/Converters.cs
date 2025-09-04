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
/// Converter for vehicle balance (placeholder - will be enhanced)
/// </summary>
public class VehicleBalanceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // This is a placeholder - in a real implementation, you'd calculate the actual balance
        // For now, return a formatted currency placeholder
        return 0m.ToString("C2");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
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
