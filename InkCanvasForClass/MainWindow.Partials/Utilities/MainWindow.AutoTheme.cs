using Microsoft.Win32;
using System;
using Ink_Canvas.Helpers;
using System.Windows;
using System.Windows.Media;
using Application = System.Windows.Application;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using static Wpf.Ui.Appearance.ApplicationThemeManager;

namespace Ink_Canvas {
    public partial class MainWindow : System.Windows.Window {
        private Color FloatBarForegroundColor = Color.FromRgb(102, 102, 102);

        private void EnsureMainWindowTransparentBackground()
        {
            if (!AllowsTransparency)
            {
                return;
            }

            Background = Brushes.Transparent;

            if (Main_Grid != null)
            {
                Main_Grid.Background = Brushes.Transparent;
            }
        }

        private void SetTheme(string theme) {
            string themeXaml = theme == "Light" ? "Resources/Styles/Light.xaml" : "Resources/Styles/Dark.xaml";
            ApplicationTheme appTheme = theme == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;

            var dictionaries = new string[] {
                themeXaml,
                "Resources/DrawShapeImageDictionary.xaml",
                "Resources/SeewoImageDictionary.xaml",
                "Resources/IconImageDictionary.xaml"
            };

            foreach (var source in dictionaries) {
                 var rd = new ResourceDictionary() { Source = new Uri(source, UriKind.Relative) };
                 Application.Current.Resources.MergedDictionaries.Add(rd);
            }

            // Transparent overlay window: avoid Wpf.Ui window background manager overriding window background.
            if (!AllowsTransparency)
            {
                Apply(appTheme, WindowBackdropType.None, false);
            }
            EnsureMainWindowTransparentBackground();

            FloatBarForegroundColor = (Color)Application.Current.FindResource("FloatBarForegroundColor");
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
            switch (Settings.Appearance.Theme) {
                case 0:
                    SetTheme("Light");
                    break;
                case 1:
                    SetTheme("Dark");
                    break;
                case 2:
                    SetTheme(IsSystemThemeLight() ? "Light" : "Dark");
                    break;
            }
        }

        private bool IsSystemThemeLight() {
            var light = false;
            try {
                var registryKey = Registry.CurrentUser;
                var themeKey =
                    registryKey.OpenSubKey("software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                var keyValue = 0;
                if (themeKey != null) keyValue = (int)themeKey.GetValue("SystemUsesLightTheme");
                if (keyValue == 1) light = true;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Failed to read system theme from registry: " + ex.Message, LogHelper.LogType.Trace);
            }

            return light;
        }
    }
}
