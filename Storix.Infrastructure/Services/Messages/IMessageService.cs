using System.Windows;

namespace Storix.Infrastructure.Services.Messages
{
    public interface IMessageService
    {
        MessageBoxResult Show( string message, string caption, MessageBoxButton button, MessageBoxImage icon );
    }
}
