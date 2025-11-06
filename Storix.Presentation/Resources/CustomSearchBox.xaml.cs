using System.Windows;
using MvvmCross.Platforms.Wpf.Views;
using MahApps.Metro.IconPacks;

namespace Storix.Presentation.Resources
{
    public partial class CustomSearchBox:MvxWpfView
    {
        public CustomSearchBox()
        {
            InitializeComponent();
        }

        // 🏷️ Placeholder text shown when empty
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                nameof(Placeholder),
                typeof(string),
                typeof(CustomSearchBox),
                new PropertyMetadata("Search..."));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        // 💬 The main data text (user input)
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(string),
                typeof(CustomSearchBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Data
        {
            get => (string)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        // 🔍 Optional: The icon used (default is Magnify)
        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(
                nameof(IconKind),
                typeof(PackIconMaterialKind),
                typeof(CustomSearchBox),
                new PropertyMetadata(PackIconMaterialKind.Magnify));

        public PackIconMaterialKind IconKind
        {
            get => (PackIconMaterialKind)GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }
    }
}
