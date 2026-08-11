using System;
using System.IO;
using System.Text.Json;

namespace PhotoViewer.Services
{
    public class MainWindowSizeSettings
    {
        public double Width { get; set; } = 1200;
        public double Height { get; set; } = 800;
    }

    /// <summary>
    /// Service responsible for saving and loading the main window size.
    /// </summary>
    public class MainWindowSizeService
    {
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public MainWindowSizeService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = Path.Combine(appDataPath, "PhotoViewer");
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }
            _settingsFilePath = Path.Combine(settingsDir, "mainWindowSize.json");
        }

        public MainWindowSizeSettings LoadSize()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    return JsonSerializer.Deserialize<MainWindowSizeSettings>(json) ?? new MainWindowSizeSettings();
                }
                catch
                {
                    return new MainWindowSizeSettings();
                }
            }
            return new MainWindowSizeSettings();
        }

        public void SaveSize(double width, double height)
        {
            try
            {
                var settings = new MainWindowSizeSettings { Width = width, Height = height };
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save main window size: {ex.Message}");
            }
        }
    }
}
