using PhotoViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows; // For MessageBox

namespace PhotoViewer.Services
{
    public class SourcePersistenceService
    {
        private readonly string _sourcesFilePath;

        public SourcePersistenceService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "PhotoViewer");
            Directory.CreateDirectory(appFolder);
            _sourcesFilePath = Path.Combine(appFolder, "sources.json");
        }

        public List<SourceConfig> LoadSources()
        {
            if (!File.Exists(_sourcesFilePath))
            {
                return new List<SourceConfig>();
            }

            try
            {
                var json = File.ReadAllText(_sourcesFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<List<SourceConfig>>(json, options);
                return result ?? new List<SourceConfig>();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading sources: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<SourceConfig>(); // On error, return empty list
            }
        }

        public void SaveSources(IEnumerable<SourceConfig> sources)
        {
            try
            {
                var list = sources.ToList();
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_sourcesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving sources: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}