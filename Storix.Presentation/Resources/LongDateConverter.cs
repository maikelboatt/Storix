using System.Globalization;
using System.Windows.Data;

namespace Storix.Presentation.Resources
{
    public class LongDateConverter:IValueConverter
    {
        public object? Convert( object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture )
        {
            if (value is DateOnly dateOnly)
            {
                // Format the date as a long date string
                return dateOnly.ToString("D", culture);
            }

            return value;
        }

        public object? ConvertBack( object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture ) => throw new NotImplementedException();
    }
}
