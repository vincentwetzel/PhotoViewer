using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PhotoViewer.Services
{
    public class HistoryService
    {
        private readonly string _historyFilePath;
        private List<string> _recentPhotoPaths;
        private const int MaxHistorySize = 100;

        public HistoryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "PhotoViewer");
            Directory.CreateDirectory(appFolder);
            _historyFilePath = Path.Combine(appFolder, "history.json");
            _recentPhotoPaths = LoadHistoryFromFile();
        }

        private List<string> LoadHistoryFromFile()
        {
            if (!File.Exists(_historyFilePath))
            {
                return new List<string>();
            }
            try
            {
                var json = File.ReadAllText(_historyFilePath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                // On error, start with a fresh history
                return new List<string>();
            }
        }

        private void SaveHistoryToFile()
        {
            var json = JsonSerializer.Serialize(_recentPhotoPaths, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyFilePath, json);
        }

        public void AddToHistory(string filePath)
        {
            // Remove if it already exists to move it to the top (most recent)
            _recentPhotoPaths.RemoveAll(p => p.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            // Add to the top of the list
            _recentPhotoPaths.Insert(0, filePath);

            // Trim the list if it exceeds the max size
            if (_recentPhotoPaths.Count > MaxHistorySize)
            {
                _recentPhotoPaths = _recentPhotoPaths.Take(MaxHistorySize).ToList();
            }

            SaveHistoryToFile();
        }

        public IEnumerable<string> GetHistory() => _recentPhotoPaths;
    }
}