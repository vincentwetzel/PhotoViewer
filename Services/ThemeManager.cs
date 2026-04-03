using System;
using System.Windows;
using Microsoft.Win32;

namespace PhotoViewer.Services
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string theme)
        {
            bool useDark = theme switch
            {
                "Light" => false,
                "Dark" => true,
                "System" => IsSystemDarkMode(),
                _ => IsSystemDarkMode()
            };

            var resources = System.Windows.Application.Current.Resources;
            
            if (useDark)
            {
                resources["WindowBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
                resources["ControlBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(43, 43, 43));
                resources["TextForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                resources["MenuBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
                resources["ListItemHover"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                
                // Dark scrollbar colors
                resources[System.Windows.SystemColors.ControlBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                resources[System.Windows.SystemColors.HighlightBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
                resources[System.Windows.SystemColors.ScrollBarBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
            }
            else
            {
                resources["WindowBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["ControlBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
                resources["TextForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
                resources["MenuBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["ListItemHover"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                
                // Light scrollbar colors
                resources[System.Windows.SystemColors.ControlBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
                resources[System.Windows.SystemColors.HighlightBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
                resources[System.Windows.SystemColors.ScrollBarBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 160, 160));
            }

            // Force refresh all windows
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                window.InvalidateVisual();
            }
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value != null && (int)value == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
