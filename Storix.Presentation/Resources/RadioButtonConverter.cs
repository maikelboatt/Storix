using System.Globalization;
using System.Windows.Data;

namespace Storix.Presentation.Resources
{
    public class RadioButtonConverter:IValueConverter
    {
        // Converts from the selected option (e.g., a string "Option1") to a bool for IsChecked
        public object Convert( object value,
            Type targetType,
            object parameter,
            CultureInfo culture )
        {
            if (value == null || parameter == null)
                return false;

            return value
                   .ToString()
                   .Equals(parameter.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        // Converts back from IsChecked to the option value
        public object ConvertBack( object value,
            Type targetType,
            object parameter,
            CultureInfo culture )
        {
            bool isChecked = (bool)value;
            return isChecked
                ? parameter.ToString()
                : Binding.DoNothing;
        }
    }
}
