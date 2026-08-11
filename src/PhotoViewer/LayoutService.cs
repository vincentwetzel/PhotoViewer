using PhotoViewer.Models;
using PhotoViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace PhotoViewer.Services
{
    /// <summary>
    /// Service responsible for saving and loading workspace layouts.
    /// Handles multi-monitor setups by storing absolute screen coordinates.
    /// </summary>
    public class LayoutService
    {
        private const string LayoutFileName = "workspaceLayout.json";
        private readonly string _layoutFilePath;
        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true // For human-readable JSON files
        };

        public LayoutService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = Path.Combine(appDataPath, "PhotoViewer");
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }
            _layoutFilePath = Path.Combine(settingsDir, LayoutFileName);
        }

        /// <summary>
        /// Saves the default workspace layout (auto-saved on exit).
        /// </summary>
        public void SaveDefaultLayout(WindowLayout layout)
        {
            SaveLayout(layout, _layoutFilePath);
        }

        /// <summary>
        /// Loads the default workspace layout (auto-saved on exit).
        /// Returns null if no saved layout exists.
        /// </summary>
        public WindowLayout? LoadDefaultLayout()
        {
            if (!File.Exists(_layoutFilePath))
                return null;

            try
            {
                var json = File.ReadAllText(_layoutFilePath);
                return JsonSerializer.Deserialize<WindowLayout>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a default layout exists.
        /// </summary>
        public bool HasDefaultLayout() => File.Exists(_layoutFilePath);

        public void SaveLayout(WindowLayout layout, string filePath)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(layout, _options);
                File.WriteAllText(filePath, jsonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save layout: {ex.Message}");
            }
        }

        public WindowLayout? LoadLayout(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<WindowLayout>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Collects state from all open PhotoWindow instances.
        /// </summary>
        public WindowLayout CollectLayout(IEnumerable<PhotoWindow> photoWindows)
        {
            var layout = new WindowLayout();
            foreach (var window in photoWindows)
            {
                var state = CollectWindowState(window);
                if (state != null)
                {
                    layout.PhotoWindows.Add(state);
                }
            }
            return layout;
        }

        /// <summary>
        /// Collects the state of a single PhotoWindow.
        /// </summary>
        private PhotoWindowState? CollectWindowState(PhotoWindow window)
        {
            // We need the file path from the ViewModel
            if (window.DataContext is not PhotoWindowViewModel vm || string.IsNullOrEmpty(vm.FilePath))
                return null;

            // Check if file still exists
            if (!File.Exists(vm.FilePath))
                return null;

            // For multi-monitor support, use absolute coordinates (Left/Top can be negative)
            // Capture restore bounds if maximized
            double left, top, width, height;
            bool isMaximized = window.WindowState == WindowState.Maximized;

            if (isMaximized)
            {
                // Use the restore bounds for position
                left = window.RestoreBounds.Left;
                top = window.RestoreBounds.Top;
                width = window.RestoreBounds.Width;
                height = window.RestoreBounds.Height;
            }
            else
            {
                left = window.Left;
                top = window.Top;
                width = window.Width;
                height = window.Height;
            }

            return new PhotoWindowState
            {
                FilePath = vm.FilePath,
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                IsMaximized = isMaximized,
                ZoomLevel = window.CurrentZoom,
                PanOffsetX = window.CurrentPanOffsetX,
                PanOffsetY = window.CurrentPanOffsetY
            };
        }

        /// <summary>
        /// Restores all PhotoWindows from a saved layout.
        /// </summary>
        public List<PhotoWindowState> RestoreLayout(string? filePath = null)
        {
            var path = filePath ?? _layoutFilePath;
            var layout = LoadLayout(path);
            if (layout == null)
                return new List<PhotoWindowState>();

            return layout.PhotoWindows;
        }
    }
}