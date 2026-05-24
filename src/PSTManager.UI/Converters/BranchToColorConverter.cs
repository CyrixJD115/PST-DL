using Avalonia.Data.Converters;
using Avalonia.Media;
using PSTManager.Core.Enums;
using System.Globalization;

namespace PSTManager.UI.Converters;

public class BranchToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BranchType branch)
        {
            return branch switch
            {
                BranchType.Stable => new SolidColorBrush(Color.Parse("#22C55E")),
                BranchType.Beta => new SolidColorBrush(Color.Parse("#EAB308")),
                _ => new SolidColorBrush(Color.Parse("#64748B"))
            };
        }
        return new SolidColorBrush(Color.Parse("#64748B"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
