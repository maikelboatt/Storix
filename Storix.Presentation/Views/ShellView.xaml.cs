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

            // Subscribe to expander toggle events with mutual exclusion
            InventoryExpander.Checked += ( s, e ) =>
            {
                // Collapse Orders if it's expanded
                if (OrdersExpander.IsChecked == true)
                {
                    OrdersExpander.IsChecked = false;
                }
                ExpandSection(InventorySubItems, InventoryArrow);
            };
            InventoryExpander.Unchecked += ( s, e ) => CollapseSection(InventorySubItems, InventoryArrow);

            OrdersExpander.Checked += ( s, e ) =>
            {
                // Collapse Inventory if it's expanded
                if (InventoryExpander.IsChecked == true)
                {
                    InventoryExpander.IsChecked = false;
                }
                ExpandSection(OrdersSubItems, OrdersArrow);
            };
            OrdersExpander.Unchecked += ( s, e ) => CollapseSection(OrdersSubItems, OrdersArrow);

            // Subscribe to toggle button state changes
            TgBtn.Checked += TgBtn_StateChanged;
            TgBtn.Unchecked += TgBtn_StateChanged;
        }

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

        private void TgBtn_StateChanged( object sender, RoutedEventArgs e )
        {
            // When collapsing, close any expanded sections
            if (TgBtn.IsChecked == false)
            {
                if (InventoryExpander.IsChecked == true)
                {
                    InventoryExpander.IsChecked = false;
                }
                if (OrdersExpander.IsChecked == true)
                {
                    OrdersExpander.IsChecked = false;
                }
            }
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

        // Start: MenuLeft PopupButton //
        private void BtnDashboard_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnDashboard;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Dashboard";
        }

        private void BtnDashboard_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnInventory_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = InventoryExpander;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Inventory";
        }

        private void BtnInventory_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnProducts_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnProducts;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Products";
        }

        private void BtnProducts_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnCategories_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnCategories;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Categories";
        }

        private void BtnCategories_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnOrders_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = OrdersExpander;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Orders";
        }

        private void BtnOrders_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnSalesOrders_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnSalesOrders;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Sales Orders";
        }

        private void BtnSalesOrders_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnPurchaseOrders_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnPurchaseOrders;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Purchase Orders";
        }

        private void BtnPurchaseOrders_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnSuppliers_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnSuppliers;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Suppliers";
        }

        private void BtnSuppliers_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnCustomers_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnCustomers;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Customers";
        }

        private void BtnCustomers_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnLocations_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnLocations;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Locations";
        }

        private void BtnLocations_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnReports_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnReports;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Reports";
        }

        private void BtnReports_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }

        private void BtnSettings_MouseEnter( object sender, MouseEventArgs e )
        {
            if (TgBtn.IsChecked != false) return;
            Popup.PlacementTarget = BtnSettings;
            Popup.Placement = PlacementMode.Right;
            Popup.IsOpen = true;
            PopupHeader.PopupText.Text = "Settings";
        }

        private void BtnSettings_MouseLeave( object sender, MouseEventArgs e )
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }
        // End: MenuLeft PopupButton //
    }
}
