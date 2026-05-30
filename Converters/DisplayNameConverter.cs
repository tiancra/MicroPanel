using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MicroPanel.Converters;

public class ServerNameOrAddressConverter : IValueConverter
{
    public static readonly ServerNameOrAddressConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string serverName && !string.IsNullOrWhiteSpace(serverName))
        {
            return serverName;
        }
        return parameter?.ToString() ?? value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
