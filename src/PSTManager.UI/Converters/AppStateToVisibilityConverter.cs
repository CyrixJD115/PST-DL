using Avalonia.Data.Converters;
using PSTManager.UI.ViewModels;
using System.Globalization;

namespace PSTManager.UI.Converters;

public class AppStateToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AppState state && parameter is string target)
        {
            var states = target.Split('|');
            foreach (var s in states)
            {
                if (Enum.TryParse<AppState>(s.Trim(), out var parsed) && state == parsed)
                    return true;
            }
            return false;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
