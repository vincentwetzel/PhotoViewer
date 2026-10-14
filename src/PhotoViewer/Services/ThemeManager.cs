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
                resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 165, 250));
                resources["AccentForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 20, 20));
                resources["SubtleTextForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(185, 185, 185));
                resources["SelectedBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 82, 105));
                resources["FocusBorder"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 165, 250));
                resources["CardBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 37, 37));
                resources["ViewerBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 17, 17));
                
                // Dark scrollbar colors
                resources[System.Windows.SystemColors.ControlBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                resources[System.Windows.SystemColors.HighlightBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
                resources[System.Windows.SystemColors.ScrollBarBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
                
                // Override menu popup colors (dropdown background/border)
                resources[System.Windows.SystemColors.MenuTextBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources[System.Windows.SystemColors.MenuBarBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
                resources[System.Windows.SystemColors.MenuHighlightBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                resources[System.Windows.SystemColors.MenuBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
                resources[System.Windows.SystemColors.WindowBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
                resources[System.Windows.SystemColors.InfoBrushKey] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            }
            else
            {
                resources["WindowBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["ControlBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
                resources["TextForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
                resources["MenuBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["ListItemHover"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 108, 189));
                resources["AccentForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["SubtleTextForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102));
                resources["SelectedBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 235, 247));
                resources["FocusBorder"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 108, 189));
                resources["CardBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["ViewerBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 17, 17));
                
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

        public static bool IsSystemDarkMode()
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
