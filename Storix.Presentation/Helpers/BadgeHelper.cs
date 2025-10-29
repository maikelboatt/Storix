using System.Windows;

namespace Storix.Presentation.Helpers
{
    public static class BadgeHelper
    {
        public static readonly DependencyProperty ShowBadgeProperty =
            DependencyProperty.RegisterAttached(
                "ShowBadge",
                typeof(bool),
                typeof(BadgeHelper),
                new PropertyMetadata(false));


        public static readonly DependencyProperty BadgeCountProperty =
            DependencyProperty.RegisterAttached(
                "BadgeCount",
                typeof(int),
                typeof(BadgeHelper),
                new PropertyMetadata(0));

        public static void SetShowBadge( UIElement element, bool value ) => element.SetValue(ShowBadgeProperty, value);

        public static bool GetShowBadge( UIElement element ) => (bool)element.GetValue(ShowBadgeProperty);

        public static void SetBadgeCount( UIElement element, int value ) => element.SetValue(BadgeCountProperty, value);

        public static int GetBadgeCount( UIElement element ) => (int)element.GetValue(BadgeCountProperty);
    }
}
