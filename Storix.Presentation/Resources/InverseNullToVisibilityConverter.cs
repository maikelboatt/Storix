using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Storix.Presentation.Resources
{
    public class InverseNullToVisibilityConverter:IValueConverter
    {
        public object? Convert( object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture ) => value == null
            ? Visibility.Visible
            : Visibility.Collapsed;

        public object? ConvertBack( object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture ) => throw new NotImplementedException();
    }
}
