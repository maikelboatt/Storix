using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Storix.Presentation.Resources
{
    public partial class CustomCards:UserControl
    {
        public CustomCards()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty CardTitleProperty =
            DependencyProperty.Register(
                nameof(CardTitle),
                typeof(string),
                typeof(CustomCards),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CardValueProperty =
            DependencyProperty.Register(
                nameof(CardValue),
                typeof(string),
                typeof(CustomCards),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ChangePercentageProperty =
            DependencyProperty.Register(
                nameof(ChangePercentage),
                typeof(string),
                typeof(CustomCards),
                new PropertyMetadata(string.Empty, OnChangePercentageChanged));

        public static readonly DependencyProperty CardImageProperty =
            DependencyProperty.Register(
                nameof(CardImage),
                typeof(ImageSource),
                typeof(CustomCards),
                new PropertyMetadata(null));

        public new static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(
                nameof(Background),
                typeof(Brush),
                typeof(CustomCards),
                new PropertyMetadata(Brushes.White));

        public new static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(
                nameof(BorderBrush),
                typeof(Brush),
                typeof(CustomCards),
                new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty IconBackgroundProperty =
            DependencyProperty.Register(
                nameof(IconBackground),
                typeof(Brush),
                typeof(CustomCards),
                new PropertyMetadata(Brushes.LightGray));

        public static readonly DependencyProperty PercentageColorProperty =
            DependencyProperty.Register(
                nameof(PercentageColor),
                typeof(Brush),
                typeof(CustomCards),
                new PropertyMetadata(Brushes.Gray));

        #endregion

        #region CLR Properties

        public string CardTitle
        {
            get => (string)GetValue(CardTitleProperty);
            set => SetValue(CardTitleProperty, value);
        }

        public string CardValue
        {
            get => (string)GetValue(CardValueProperty);
            set => SetValue(CardValueProperty, value);
        }

        public string ChangePercentage
        {
            get => (string)GetValue(ChangePercentageProperty);
            set => SetValue(ChangePercentageProperty, value);
        }

        public ImageSource CardImage
        {
            get => (ImageSource)GetValue(CardImageProperty);
            set => SetValue(CardImageProperty, value);
        }

        public new Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public new Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public Brush IconBackground
        {
            get => (Brush)GetValue(IconBackgroundProperty);
            set => SetValue(IconBackgroundProperty, value);
        }

        public Brush PercentageColor
        {
            get => (Brush)GetValue(PercentageColorProperty);
            set => SetValue(PercentageColorProperty, value);
        }

        #endregion

        #region Logic

        private static void OnChangePercentageChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            if (d is not CustomCards card) return;

            string? value = e.NewValue as string ?? string.Empty;
            string normalized = value
                                .Trim()
                                .ToLowerInvariant();

            if (normalized.Contains("↑") || normalized.Contains("+"))
                card.PercentageColor = new SolidColorBrush(Color.FromRgb(30, 142, 62)); // green
            else if (normalized.Contains("↓") || normalized.Contains("-"))
                card.PercentageColor = new SolidColorBrush(Color.FromRgb(217, 48, 37)); // red
            else
                card.PercentageColor = Brushes.Gray;
        }

        #endregion
    }
}
