using System.Windows;

namespace Storix.Presentation.Themes
{
    public static class ThemesController
    {
        public static ThemeTypes CurrentTheme { get; set; } = ThemeTypes.Light;

        public static void SetTheme( ThemeTypes theme )
        {
            CurrentTheme = theme;

            string themeName = theme switch
            {
                ThemeTypes.Light => "LightTheme",
                ThemeTypes.Dark  => "DarkTheme",
                _                => "LightTheme"
            };

            try
            {
                System.Windows.Application? app = System.Windows.Application.Current;

                // Get the main resource dictionary
                if (app.Resources.MergedDictionaries.Count > 0)
                {
                    ResourceDictionary? mainDict = app.Resources.MergedDictionaries[0];

                    // Create the new theme dictionary with proper URI
                    Uri themeUri = new($"/Storix.Presentation;component/Themes/{themeName}.xaml", UriKind.Relative);
                    ResourceDictionary newTheme = new()
                    {
                        Source = themeUri
                    };

                    // Replace the theme (it's the first in MergedDictionaries)
                    if (mainDict.MergedDictionaries.Count > 0)
                    {
                        mainDict.MergedDictionaries[0] = newTheme;
                    }
                    else
                    {
                        mainDict.MergedDictionaries.Insert(0, newTheme);
                    }

                    // Force refresh
                    app.Resources.MergedDictionaries.Remove(mainDict);
                    app.Resources.MergedDictionaries.Insert(0, mainDict);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error switching theme: {e.Message}");
                Console.WriteLine($"Stack trace: {e.StackTrace}");
            }
        }
    }
}
