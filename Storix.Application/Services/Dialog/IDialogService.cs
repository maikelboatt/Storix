namespace Storix.Application.Services.Dialog
{
    public interface IDialogService
    {
        void ShowError( string message, string caption = "Error" );

        void ShowWarning( string message, string caption = "Warning" );

        void ShowInfo( string message, string caption = "Information" );

        bool ShowConfirmation( string message, string caption = "Confirm" );
    }
}
