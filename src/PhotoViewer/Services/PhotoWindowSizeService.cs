using System;
using System.IO;
using System.Text.Json;

namespace PhotoViewer.Services
{
    public class PhotoWindowSizeSettings
    {
        public double Width { get; set; } = 800;
        public double Height { get; set; } = 600;
    }

    /// <summary>
    /// Service responsible for saving and loading the default photo window size.
    /// </summary>
    public class PhotoWindowSizeService
    {
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public PhotoWindowSizeService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = Path.Combine(appDataPath, "PhotoViewer");
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }
            _settingsFilePath = Path.Combine(settingsDir, "photoWindowSize.json");
        }

        public PhotoWindowSizeSettings LoadSize()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    return JsonSerializer.Deserialize<PhotoWindowSizeSettings>(json) ?? new PhotoWindowSizeSettings();
                }
                catch
                {
                    return new PhotoWindowSizeSettings();
                }
            }
            return new PhotoWindowSizeSettings();
        }

        public void SaveSize(double width, double height)
        {
            try
            {
                var settings = new PhotoWindowSizeSettings { Width = width, Height = height };
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save photo window size: {ex.Message}");
            }
        }
    }
}
