using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MvvmCross.Commands;
using MvvmCross.Platforms.Wpf.Views;

namespace Storix.Presentation.Resources
{
    public partial class NavigationItem:MvxWpfView
    {
        public NavigationItem()
        {
            InitializeComponent();
            Loaded += NavigationItem_Loaded;
        }

        private void NavigationItem_Loaded( object sender, RoutedEventArgs e )
        {
            UpdateMenuButtonStyle();
        }

        private void UpdateMenuButtonStyle()
        {
            if (MenuButton != null)
            {
                Style style = IsSubMenuItem
                    ? (Style)TryFindResource("SubMenuButtonStyle")
                    : (Style)TryFindResource("MenuButtonStyle");

                if (style != null)
                {
                    MenuButton.Style = style;
                }
            }
        }

        /// DependencyProperty for the Icon property
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(PathGeometry),
            typeof(NavigationItem),
            new PropertyMetadata(default(PathGeometry)));

        public PathGeometry Icon
        {
            get => (PathGeometry)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // Dependency Property for the IconWidth property
        public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register(
            nameof(IconWidth),
            typeof(int),
            typeof(NavigationItem),
            new PropertyMetadata(default(int)));

        public int IconWidth
        {
            get => (int)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        // Dependency Property for the IconMargin property
        public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register(
            nameof(IconMargin),
            typeof(Thickness),
            typeof(NavigationItem),
            new PropertyMetadata(new Thickness(0)));

        public Thickness IconMargin
        {
            get => (Thickness)GetValue(IconMarginProperty);
            set => SetValue(IconMarginProperty, value);
        }

        public SolidColorBrush IndicatorBrush
        {
            get => (SolidColorBrush)GetValue(IndicatorBrushProperty);
            set => SetValue(IndicatorBrushProperty, value);
        }

        // Using a DependencyProperty as the backing store for IndicatorBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IndicatorBrushProperty =
            DependencyProperty.Register(nameof(IndicatorBrush), typeof(SolidColorBrush), typeof(NavigationItem));


        public int IndicatorIndicatorCornerRadius
        {
            get => (int)GetValue(IndicatorIndicatorCornerRadiusProperty);
            set => SetValue(IndicatorIndicatorCornerRadiusProperty, value);
        }

        // Using a DependencyProperty as the backing store for IndicatorIndicatorCornerRadius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IndicatorIndicatorCornerRadiusProperty =
            DependencyProperty.Register(nameof(IndicatorIndicatorCornerRadius), typeof(int), typeof(NavigationItem));


        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(NavigationItem));


        public new Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        // Using a DependencyProperty as the backing store for Padding.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(NavigationItem));


        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(NavigationItem));


        public string GroupName
        {
            get => (string)GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }

        // Using a DependencyProperty as the backing store for GroupName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(NavigationItem));


        public static readonly DependencyProperty CommandNameProperty = DependencyProperty.Register(
            nameof(CommandName),
            typeof(MvxCommand),
            typeof(NavigationItem));

        public MvxCommand CommandName
        {
            get => (MvxCommand)GetValue(CommandNameProperty);
            set => SetValue(CommandNameProperty, value);
        }

        // NEW: Dependency Property for IsSubMenuItem with property changed callback
        public static readonly DependencyProperty IsSubMenuItemProperty = DependencyProperty.Register(
            nameof(IsSubMenuItem),
            typeof(bool),
            typeof(NavigationItem),
            new PropertyMetadata(false, OnIsSubMenuItemChanged));

        public bool IsSubMenuItem
        {
            get => (bool)GetValue(IsSubMenuItemProperty);
            set => SetValue(IsSubMenuItemProperty, value);
        }

        private static void OnIsSubMenuItemChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            if (d is NavigationItem navigationItem)
            {
                navigationItem.UpdateMenuButtonStyle();
            }
        }
    }
}
