using System.Windows;
using System.Windows.Controls;
using MvvmCross.Platforms.Wpf.Views;

namespace Storix.Presentation.Resources
{
    public partial class CustomSearchBox:MvxWpfView
    {
        /// <summary>
        ///     Dependency property for the placeholder text.
        /// </summary>
        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(CustomTextBox),
            new PropertyMetadata(default(string)));

        /// <summary>
        ///     Dependency property for the data value.
        /// </summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data),
            typeof(string),
            typeof(CustomTextBox),
            new PropertyMetadata(default(string)));

        public CustomSearchBox()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether to check if the text box is empty.
        /// </summary>
        public bool CheckEmpty { get; set; }

        /// <summary>
        ///     Gets or sets the placeholder text for the text box.
        /// </summary>
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        /// <summary>
        ///     Gets or sets the data value for the text box.
        /// </summary>
        public string Data
        {
            get => (string)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}
