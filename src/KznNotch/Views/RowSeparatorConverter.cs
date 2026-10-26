using System.Globalization;
using System.Windows.Data;

namespace KznNotch.Views;

public sealed class RowSeparatorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is int index && values[1] is int count && index == count - 1)
            return new Thickness(0);
        return new Thickness(0, 0, 0, 1);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
