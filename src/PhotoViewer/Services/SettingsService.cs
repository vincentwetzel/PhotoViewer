using System;
using System.IO;
using System.Text.Json;

namespace PhotoViewer.Services
{
    public class AppSettings
    {
        public int Version { get; set; } = 2;
        public string Theme { get; set; } = "System"; // Light, Dark, System
        public string SortOption { get; set; } = "Name";
        public string SortDirection { get; set; } = "Ascending";
        public string MediaType { get; set; } = "All media";
        public double ThumbnailRowHeight { get; set; } = 180;
        public bool ConfirmDelete { get; set; } = true;
        public bool MouseWheelZoom { get; set; } = true;
        public bool ShowFileLocation { get; set; } = true;
        public bool AllowAnonymousDiagnostics { get; set; } = false;
        public bool PersonalizedExperiences { get; set; } = false;
        public bool ReduceMotion { get; set; } = false;
        public double DefaultViewerZoom { get; set; } = 1.0;
    }

    public class SettingsService
    {
        private readonly string _settingsFilePath;

        public SettingsService(string? settingsDirectory = null)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = settingsDirectory ?? Path.Combine(appDataPath, "PhotoViewer");
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }
            _settingsFilePath = Path.Combine(settingsDir, "settings.json");
        }

        public AppSettings LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new AppSettings();
                    using var document = JsonDocument.Parse(json);
                    var hasVersion = document.RootElement.ValueKind == JsonValueKind.Object
                        && document.RootElement.TryGetProperty("Version", out _);
                    if (!hasVersion || settings.Version < 2)
                    {
                        // Version 1 only stored Theme. New properties intentionally
                        // retain their safe defaults during migration.
                        settings.Version = 2;
                        SaveSettings(settings);
                    }
                    return settings;
                }
                catch
                {
                    return new AppSettings();
                }
            }
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            var temporaryPath = _settingsFilePath + ".tmp";
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, _settingsFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
                try
                {
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
                catch { }
            }
        }
    }
}
