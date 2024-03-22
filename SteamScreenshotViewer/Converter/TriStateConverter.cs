using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SteamScreenshotViewer.Converter;

public class TriStateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // bool? -> bool
        if (value == null)
        {
            return false;
        }

        if (value is bool b)
        {
            return b;
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // bool -> bool?
        if (value is bool b)
        {
            return b;
        }

        return DependencyProperty.UnsetValue;
    }
}