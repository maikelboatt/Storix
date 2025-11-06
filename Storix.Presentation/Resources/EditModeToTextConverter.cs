using System.Globalization;
using System.Windows.Data;

namespace Storix.Presentation.Resources
{
    public class EditModeToTextConverter:IValueConverter
    {
        public object Convert( object value,
            Type targetType,
            object parameter,
            CultureInfo culture )
        {
            bool isEditMode = value is bool b && b;
            return isEditMode
                ? "Updating product..."
                : "Creating product...";
        }

        public object ConvertBack( object value,
            Type targetType,
            object parameter,
            CultureInfo culture ) => throw new NotImplementedException();
    }
}
