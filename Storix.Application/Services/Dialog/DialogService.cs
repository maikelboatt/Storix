using System.Windows;

namespace Storix.Application.Services.Dialog
{
    public class DialogService:IDialogService
    {
        public void ShowError( string message, string caption = "Error" )
        {
            MessageBox.Show(
                message,
                caption,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public void ShowWarning( string message, string caption = "Warning" )
        {
            MessageBox.Show(
                message,
                caption,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        public void ShowInfo( string message, string caption = "Information" )
        {
            MessageBox.Show(
                message,
                caption,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public bool ShowConfirmation( string message, string caption = "Confirm" )
        {
            MessageBoxResult result = MessageBox.Show(
                message,
                caption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
