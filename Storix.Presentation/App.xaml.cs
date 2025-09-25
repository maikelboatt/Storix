using System.Windows;
using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Views;

namespace Storix.Presentation
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App:MvxApplication
    {
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<Setup>();
        }

        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup(e);
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}
