using System.Windows;
using System.Windows.Controls;
using MvvmCross.Platforms.Wpf.Views;

namespace Storix.Presentation.Resources
{
    public partial class CustomIntegerBox:MvxWpfView
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data),
            typeof(string),
            typeof(CustomIntegerBox),
            new PropertyMetadata(default(string)));

        public static readonly DependencyProperty BiggestProperty = DependencyProperty.Register(
            nameof(Biggest),
            typeof(int),
            typeof(CustomIntegerBox),
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty SmallestProperty = DependencyProperty.Register(
            nameof(Smallest),
            typeof(int),
            typeof(CustomIntegerBox),
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(CustomIntegerBox),
            new PropertyMetadata(default(string)));

        public CustomIntegerBox()
        {
            InitializeComponent();
        }

        public int Biggest
        {
            get => (int)GetValue(BiggestProperty);
            set => SetValue(BiggestProperty, value);
        }

        public int Smallest
        {
            get => (int)GetValue(SmallestProperty);
            set => SetValue(SmallestProperty, value);
        }

        public string Data
        {
            get => (string)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
    }
}
