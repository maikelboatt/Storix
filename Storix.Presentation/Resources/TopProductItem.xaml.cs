using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MvvmCross.Platforms.Wpf.Views;

namespace Storix.Presentation.Resources
{
    public partial class TopProductItem:MvxWpfView
    {
        public TopProductItem()
        {
            InitializeComponent();
        }

        #region Rank Property

        public static readonly DependencyProperty RankProperty =
            DependencyProperty.Register(
                nameof(Rank),
                typeof(int),
                typeof(TopProductItem),
                new PropertyMetadata(1, OnRankChanged));

        public int Rank
        {
            get => (int)GetValue(RankProperty);
            set => SetValue(RankProperty, value);
        }

        private static void OnRankChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            if (d is TopProductItem control)
            {
                control.UpdateRankColor();
            }
        }

        #endregion

        #region ProductName Property

        public static readonly DependencyProperty ProductNameProperty =
            DependencyProperty.Register(
                nameof(ProductName),
                typeof(string),
                typeof(TopProductItem),
                new PropertyMetadata(string.Empty));

        public string ProductName
        {
            get => (string)GetValue(ProductNameProperty);
            set => SetValue(ProductNameProperty, value);
        }

        #endregion

        #region UnitsSold Property

        public static readonly DependencyProperty UnitsSoldProperty =
            DependencyProperty.Register(
                nameof(UnitsSold),
                typeof(int),
                typeof(TopProductItem),
                new PropertyMetadata(0));

        public int UnitsSold
        {
            get => (int)GetValue(UnitsSoldProperty);
            set => SetValue(UnitsSoldProperty, value);
        }

        #endregion

        #region Revenue Property

        public static readonly DependencyProperty RevenueProperty =
            DependencyProperty.Register(
                nameof(Revenue),
                typeof(string),
                typeof(TopProductItem),
                new PropertyMetadata("$0"));

        public string Revenue
        {
            get => (string)GetValue(RevenueProperty);
            set => SetValue(RevenueProperty, value);
        }

        #endregion

        #region RankColor Property

        public static readonly DependencyProperty RankColorProperty =
            DependencyProperty.Register(
                nameof(RankColor),
                typeof(Brush),
                typeof(TopProductItem),
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"))));

        public Brush RankColor
        {
            get => (Brush)GetValue(RankColorProperty);
            private set => SetValue(RankColorProperty, value);
        }

        #endregion

        #region Private Methods

        private void UpdateRankColor()
        {
            RankColor = Rank switch
            {
                1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")), // Blue
                2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6")), // Purple
                3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")), // Orange
                4 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06B6D4")), // Cyan
                5 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")), // Red
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))  // Gray (fallback)
            };
        }

        #endregion
    }
}
