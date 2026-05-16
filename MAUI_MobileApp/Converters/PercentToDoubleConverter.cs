using System.Globalization;

namespace SmartGreenhouseApp.Converters;

/// <summary>
/// Converts an integer percentage (0-100) to a double (0.0-1.0) for ProgressBar.
/// </summary>
public class PercentToDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intVal)
            return intVal / 100.0;
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleVal)
            return (int)(doubleVal * 100);
        return 0;
    }
}
