using System;
using System.Windows;

namespace Storix.Presentation.Themes
{
    /// <summary>
    /// Controller class for managing application theme switching
    /// </summary>
    public static class ThemesController
    {
        private const string LightThemeUri = "Themes/LightTheme.xaml";
        private const string DarkThemeUri = "Themes/DarkTheme.xaml";

        /// <summary>
        /// Sets the application theme
        /// </summary>
        /// <param name="theme">The theme type to apply</param>
        public static void SetTheme( ThemeTypes theme )
        {
            string themeSource = theme == ThemeTypes.Dark
                ? DarkThemeUri
                : LightThemeUri;

            // Create a new ResourceDictionary from the theme file
            ResourceDictionary themeDict = new()
            {
                Source = new Uri(themeSource, UriKind.Relative)
            };

            // Get the application resources
            ResourceDictionary appResources = System.Windows.Application.Current.Resources;

            // Find and remove existing theme dictionary
            ResourceDictionary? existingTheme = null;
            foreach (ResourceDictionary dict in appResources.MergedDictionaries)
            {
                if (dict.Source != null &&
                    (dict.Source.OriginalString.Contains("LightTheme.xaml") ||
                     dict.Source.OriginalString.Contains("DarkTheme.xaml")))
                {
                    existingTheme = dict;
                    break;
                }
            }

            // Remove old theme
            if (existingTheme != null)
            {
                appResources.MergedDictionaries.Remove(existingTheme);
            }

            // Add new theme
            appResources.MergedDictionaries.Add(themeDict);
        }

        /// <summary>
        /// Gets the current theme type
        /// </summary>
        /// <returns>The current theme type</returns>
        public static ThemeTypes GetCurrentTheme()
        {
            ResourceDictionary appResources = System.Windows.Application.Current.Resources;

            foreach (ResourceDictionary dict in appResources.MergedDictionaries)
            {
                if (dict.Source != null && dict.Source.OriginalString.Contains("DarkTheme.xaml"))
                {
                    return ThemeTypes.Dark;
                }
            }

            return ThemeTypes.Light;
        }

        /// <summary>
        /// Toggles between light and dark themes
        /// </summary>
        public static void ToggleTheme()
        {
            ThemeTypes currentTheme = GetCurrentTheme();
            ThemeTypes newTheme = currentTheme == ThemeTypes.Light
                ? ThemeTypes.Dark
                : ThemeTypes.Light;
            SetTheme(newTheme);
        }
    }
}
