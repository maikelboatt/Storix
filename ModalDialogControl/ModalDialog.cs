using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ModalDialogControl
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:ModalDialogControl"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:ModalDialogControl;assembly=ModalDialogControl"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right-click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:ModalDialog/>
    ///
    /// </summary>
    public class ModalDialog:ContentControl
    {
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(ModalDialog),
            new PropertyMetadata(false));

        public static readonly DependencyProperty TheBackgroundProperty = DependencyProperty.Register(
            nameof(TheBackground),
            typeof(SolidColorBrush),
            typeof(ModalDialog),
            new PropertyMetadata(default(SolidColorBrush)));

        public static readonly DependencyProperty TheShadowProperty = DependencyProperty.Register(
            nameof(TheShadow),
            typeof(SolidColorBrush),
            typeof(ModalDialog),
            new PropertyMetadata(default(SolidColorBrush)));

        private static SolidColorBrush CreateBackground() => new(Colors.Black)
        {
            Opacity = 0.3
        };


        static ModalDialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ModalDialog), new FrameworkPropertyMetadata(typeof(ModalDialog)));
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }
        public SolidColorBrush TheBackground
        {
            get => (SolidColorBrush)GetValue(TheBackgroundProperty);
            set => SetValue(TheBackgroundProperty, value);
        }


        public SolidColorBrush TheShadow
        {
            get => (SolidColorBrush)GetValue(TheShadowProperty);
            set => SetValue(TheShadowProperty, value);
        }
    }
}
