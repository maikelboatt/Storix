using System.Windows;

namespace Storix.Infrastructure.Services.Messages
{
    public class MessageService:IMessageService
    {
        public MessageBoxResult Show( string message, string caption, MessageBoxButton button, MessageBoxImage icon ) => MessageBox.Show(message, caption, button, icon);
    }
}
