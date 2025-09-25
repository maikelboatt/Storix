using System.Windows;
using MvvmCross.Platforms.Wpf.Views;

namespace Storix.Presentation.Views
{
    public partial class ShellView:MvxWpfView
    {
        private Window? _parentWindow;

        public ShellView()
        {
            _parentWindow = Window.GetWindow(this);
            InitializeComponent();
        }

        private void ShellView_OnLoaded( object sender, RoutedEventArgs e )
        {
            _parentWindow = Window.GetWindow(this);
        }
    }
}
