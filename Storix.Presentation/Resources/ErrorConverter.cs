using System.Globalization;
using System.Windows.Data;

namespace Storix.Presentation.Resources
{
    public class ErrorConverter:IValueConverter
    {
        public object Convert( object value,
            Type targetType,
            object parameter,
            CultureInfo culture )
        {
            if (value is IEnumerable<string> errors && errors.Any())
            {
                // Convert the collection of error messages into a single string.
                // You can customize how the errors are formatted.
                return string.Join(Environment.NewLine, errors); // Example: Join with newlines
                // Or:
                // return string.Join(", ", errors); // Example: Join with commas
                // Or:
                // return errors.ToList(); // Example: Return the list itself (if your UI can bind to a collection)
            }

            return null; // Return null if there are no errors
        }

        public object ConvertBack( object value,
            Type targetType,
            object parameter,
            CultureInfo culture ) => throw new NotImplementedException(); // Usually not needed for error display
    }
}
