using System.Windows;
using System.Windows.Controls;
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

        private void Border_OnMouseDown( object sender, MouseButtonEventArgs e )
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                _parentWindow.DragMove();
        }

        private void UIElement_OnMouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            _parentWindow.DragMove();
        }

        // private void ToggleNavButton_Click( object sender, RoutedEventArgs e )
        // {
        //     if (_isNavExpanded)
        //     {
        //         // Collapse NavBar
        //         AnimateNavWidth(250, 70);
        //         LogoPanel.Visibility = Visibility.Collapsed;
        //
        //         foreach (RadioButton child in NavButtonsPanel.Children.OfType<RadioButton>())
        //         {
        //             if (child.Content is string label)
        //             {
        //                 child.ToolTip = child.Tag; // Add tooltip when collapsed
        //                 child.Content = null;      // Hide label text
        //             }
        //         }
        //     }
        //     else
        //     {
        //         // Expand NavBar
        //         AnimateNavWidth(70, 250);
        //         LogoPanel.Visibility = Visibility.Visible;
        //
        //         List<RadioButton> buttons = NavButtonsPanel
        //                                     .Children.OfType<RadioButton>()
        //                                     .ToList();
        //         string[] labels =
        //         {
        //             "Analytics",
        //             "Home",
        //             "Inventory",
        //             "Orders",
        //             "Reminders",
        //             "Notifications"
        //         };
        //
        //         for (int i = 0; i < Math.Min(buttons.Count, labels.Length); i++)
        //         {
        //             buttons[i].Content = labels[i];
        //             buttons[i].ToolTip = null; // Remove tooltip when expanded
        //         }
        //     }
        //
        //     _isNavExpanded = !_isNavExpanded;
        // }

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
                // AnimateNavWidth(250, 70);
                // Collapse
                NavColumn.Width = new GridLength(70);
                LogoPanel.Visibility = Visibility.Collapsed;

                foreach (RadioButton child in NavButtonsPanel.Children.OfType<RadioButton>())
                {
                    if (child.Content is string)
                        child.Content = null; // Hide labels
                }
            }
            else
            {
                // AnimateNavWidth(70, 250);
                // Expand
                NavColumn.Width = new GridLength(250);
                LogoPanel.Visibility = Visibility.Visible;

                // Restore labels (match your XAML order)
                List<RadioButton> buttons = NavButtonsPanel
                                            .Children.OfType<RadioButton>()
                                            .ToList();

                if (buttons.Count >= 6)
                {
                    buttons[0].Content = "Analytics";
                    buttons[1].Content = "Home";
                    buttons[2].Content = "Inventory";
                    buttons[3].Content = "Orders";
                    buttons[4].Content = "Reminders";
                    buttons[5].Content = "Notifications";
                }
            }

            _isNavExpanded = !_isNavExpanded;
        }
    }
}
