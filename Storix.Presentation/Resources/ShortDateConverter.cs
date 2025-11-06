using System.Globalization;
using System.Windows.Data;

namespace Storix.Presentation.Resources
{
    public class ShortDateConverter:IValueConverter
    {
        public object? Convert( object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture )
        {
            if (value is DateOnly dateOnly)
            {
                // Format the date as a long date string
                return dateOnly.ToString("d", culture);
            }

            return value;
        }

        public object? ConvertBack( object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture ) => throw new NotImplementedException();
    }
}
