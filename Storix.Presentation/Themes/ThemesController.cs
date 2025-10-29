using System.Windows;

namespace Storix.Presentation.Themes
{
    public static class ThemesController
    {
        public static ThemeTypes CurrentTheme { get; set; }

        public static ResourceDictionary ThemeDictionary
        {
            get => System.Windows.Application.Current.Resources.MergedDictionaries[0];
            set => System.Windows.Application.Current.Resources.MergedDictionaries[0] = value;
        }

        private static void ChangeTheme( Uri uri )
        {
            ThemeDictionary = new ResourceDictionary
            {
                Source = uri
            };
        }

        public static void SetTheme( ThemeTypes theme )
        {
            string themeName = null!;
            CurrentTheme = theme;

            themeName = theme switch
            {
                ThemeTypes.Light => "LightTheme",
                ThemeTypes.Dark  => "DarkTheme",
                _                => themeName
            };

            try
            {
                if (!string.IsNullOrEmpty(themeName))
                {
                    ChangeTheme(new Uri($"Themes/{themeName}.xaml"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
