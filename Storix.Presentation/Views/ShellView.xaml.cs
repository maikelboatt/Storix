using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MvvmCross.Platforms.Wpf.Views;
using Storix.Presentation.Themes;

namespace Storix.Presentation.Views
{
    public partial class ShellView:MvxWpfView
    {
        private Window? _parentWindow;

        public ShellView()
        {
            _parentWindow = Window.GetWindow(this);
            InitializeComponent();

            // Subscribe to expander toggle events
            InventoryExpander.Checked += ( s, e ) => ExpandSection(InventorySubItems, InventoryArrow);
            InventoryExpander.Unchecked += ( s, e ) => CollapseSection(InventorySubItems, InventoryArrow);

            OrdersExpander.Checked += ( s, e ) => ExpandSection(OrdersSubItems, OrdersArrow);
            OrdersExpander.Unchecked += ( s, e ) => CollapseSection(OrdersSubItems, OrdersArrow);

        }

        private bool _isNavExpanded = true;

        private void ExpandSection( StackPanel subItems, RotateTransform arrow )
        {
            // Show sub-items with animation
            subItems.Visibility = Visibility.Visible;

            // Animate arrow rotation
            DoubleAnimation rotateAnimation = new()
            {
                From = 0,
                To = 180,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };
            arrow.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        private void CollapseSection( StackPanel subItems, RotateTransform arrow )
        {
            // Animate arrow rotation back
            DoubleAnimation rotateAnimation = new()
            {
                From = 180,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };
            arrow.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

            // Hide sub-items after animation
            subItems.Visibility = Visibility.Collapsed;
        }


        private void Themes_Click( object sender, RoutedEventArgs e )
        {
            ThemesController.SetTheme(
                Themes.IsChecked == true
                    ? ThemeTypes.Dark
                    : ThemeTypes.Light);
        }

        private void CloseButton_OnClick( object sender, RoutedEventArgs e )
        {
            _parentWindow?.Close();
        }


        private void RestoreButton_OnClick( object sender, RoutedEventArgs e )
        {
            Window? window = Window.GetWindow(this);
            if (window == null) return;

            if (_parentWindow.WindowState == WindowState.Maximized)
            {
                // Restore down
                window.WindowState = WindowState.Normal;
                RestoreButton.Content = FindResource("Maximize"); // your maximize icon
            }
            else
            {
                // Maximize
                window.WindowState = WindowState.Maximized;
                RestoreButton.Content = FindResource("Restore"); // your restore icon
            }

        }

        private void MinimizeButton_OnClick( object sender, RoutedEventArgs e )
        {
            _parentWindow.WindowState = WindowState.Minimized;
        }

        private void ShellView_OnLoaded( object sender, RoutedEventArgs e )
        {
            _parentWindow = Window.GetWindow(this);
        }

        private void LogoArea_OnMouseDown( object sender, MouseButtonEventArgs e )
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _parentWindow?.DragMove();
            }
        }

        private void TopBar_OnMouseDown( object sender, MouseButtonEventArgs e )
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _parentWindow?.DragMove();
            }
        }

        private void AnimateNavWidth( double from, double to )
        {
            // Build the animation (duration & easing)
            DoubleAnimation animation = new()
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            // Create an AnimationClock from the animation
            AnimationClock clock = animation.CreateClock();

            // Handler: update NavColumn.Width each tick/frame
            EventHandler handler = null;
            handler = ( s, e ) =>
            {
                // CurrentProgress is a double? nullable between 0..1
                double progress = clock.CurrentProgress ?? 0.0;
                double current = from + (to - from) * progress;
                NavColumn.Width = new GridLength(current);
            };

            // Completed handler to ensure exact final width and detach handlers
            EventHandler completedHandler = null;
            completedHandler = ( s, e ) =>
            {
                // finalize exact width
                NavColumn.Width = new GridLength(to);

                // Detach handlers to avoid memory leaks
                clock.CurrentTimeInvalidated -= handler;
                clock.Completed -= completedHandler;
            };

            // Attach handlers
            clock.CurrentTimeInvalidated += handler;
            clock.Completed += completedHandler;

            // Start the clock (this drives the animation)
            clock.Controller?.Begin();
        }

        private void ToggleNavButton_Click( object sender, RoutedEventArgs e )
        {
            if (_isNavExpanded)
            {
                // Collapse NavBar
                AnimateNavWidth(250, 70);
                LogoPanel.Visibility = Visibility.Collapsed;
                SloganText.Visibility = Visibility.Collapsed;

                // Update all navigation items
                UpdateNavigationItems(true);
            }
            else
            {
                // Expand NavBar
                AnimateNavWidth(70, 250);
                LogoPanel.Visibility = Visibility.Visible;
                SloganText.Visibility = Visibility.Visible;

                // Update all navigation items
                UpdateNavigationItems(false);
            }

            _isNavExpanded = !_isNavExpanded;
        }

        private void UpdateNavigationItems( bool isCollapsed )
        {
            foreach (object? child in NavButtonsPanel.Children)
            {
                // Handle regular RadioButtons (Dashboard, Suppliers, Customers, etc.)
                if (child is RadioButton radioButton)
                {
                    if (isCollapsed)
                    {
                        // Store the current content as tooltip and hide content
                        if (radioButton.Content is string content)
                        {
                            radioButton.ToolTip = CreateStyledToolTip(content);
                            radioButton.Content = null;
                        }
                    }
                    else
                    {
                        // Restore content from tooltip and remove tooltip
                        if (radioButton.ToolTip is ToolTip toolTip && toolTip.Content is string text)
                        {
                            radioButton.Content = text;
                            radioButton.ToolTip = null;
                        }
                    }
                }
                // Handle StackPanels (Inventory and Orders expandable sections)
                else if (child is StackPanel stackPanel)
                {
                    foreach (object? subChild in stackPanel.Children)
                    {
                        // Handle ToggleButtons (Inventory, Orders expanders)
                        if (subChild is ToggleButton toggleButton)
                        {
                            if (isCollapsed)
                            {
                                // Extract text from the Grid's TextBlock
                                string text = GetToggleButtonText(toggleButton);
                                if (!string.IsNullOrEmpty(text))
                                {
                                    toggleButton.ToolTip = CreateStyledToolTip(text);
                                }
                            }
                            else
                            {
                                toggleButton.ToolTip = null;
                            }
                        }
                        // Handle sub-item StackPanels (Products, Categories, Sales Orders, Purchase Orders)
                        else if (subChild is StackPanel subItemPanel)
                        {
                            foreach (object? subItem in subItemPanel.Children)
                            {
                                if (subItem is RadioButton subRadioButton)
                                {
                                    if (isCollapsed)
                                    {
                                        // Store the current content as tooltip and hide content
                                        if (subRadioButton.Content is string subContent)
                                        {
                                            subRadioButton.ToolTip = CreateStyledToolTip(subContent);
                                            subRadioButton.Content = null;
                                        }
                                    }
                                    else
                                    {
                                        // Restore content from tooltip and remove tooltip
                                        if (subRadioButton.ToolTip is ToolTip subToolTip && subToolTip.Content is string subText)
                                        {
                                            subRadioButton.Content = subText;
                                            subRadioButton.ToolTip = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // Handle Separator - skip it
                else if (child is Separator)
                {
                    continue;
                }
            }
        }

        private string GetToggleButtonText( ToggleButton toggleButton )
        {
            // Extract text from the ToggleButton's Grid structure
            if (toggleButton.Content is Grid grid)
            {
                foreach (object? child in grid.Children)
                {
                    if (child is TextBlock textBlock)
                    {
                        return textBlock.Text;
                    }
                }
            }
            return string.Empty;
        }

        private ToolTip CreateStyledToolTip( string content )
        {
            // Create a styled ToolTip that matches the UI design
            ToolTip toolTip = new()
            {
                Content = content,
                Placement = PlacementMode.Right,
                HorizontalOffset = 10,
                VerticalOffset = 0
            };

            // Try to apply the style from resources
            if (TryFindResource("NavBarToolTipStyle") is Style style)
            {
                toolTip.Style = style;
            }
            else
            {
                // Fallback: Apply inline styling
                toolTip.Background = (Brush)TryFindResource("TertiaryBackgroundColor") ?? new SolidColorBrush(Color.FromRgb(51, 65, 85));
                toolTip.Foreground = (Brush)TryFindResource("PrimaryWhiteColor") ?? Brushes.White;
                toolTip.BorderBrush = (Brush)TryFindResource("SecondaryBlueColor") ?? new SolidColorBrush(Color.FromRgb(59, 130, 246));
                toolTip.BorderThickness = new Thickness(1);
                toolTip.Padding = new Thickness(
                    12,
                    8,
                    12,
                    8);
                toolTip.FontSize = 13;
                toolTip.FontWeight = FontWeights.Medium;
                toolTip.HasDropShadow = true;
            }

            return toolTip;
        }
    }
}
