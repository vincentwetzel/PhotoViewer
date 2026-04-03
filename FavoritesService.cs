using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PhotoViewer.Services
{
    public class FavoritesService
    {
        private readonly string _favoritesFilePath;
        private HashSet<string> _favoritePhotoPaths;

        public FavoritesService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "PhotoViewer");
            Directory.CreateDirectory(appFolder); // Ensure the directory exists
            _favoritesFilePath = Path.Combine(appFolder, "favorites.json");

            _favoritePhotoPaths = LoadFavoritesFromFile();
        }

        private HashSet<string> LoadFavoritesFromFile()
        {
            if (!File.Exists(_favoritesFilePath))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var json = File.ReadAllText(_favoritesFilePath);
                var paths = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                return new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                // In case of corruption or error, start with a fresh set.
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void SaveFavoritesToFile()
        {
            var json = JsonSerializer.Serialize(_favoritePhotoPaths.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_favoritesFilePath, json);
        }

        public bool IsFavorite(string filePath) => _favoritePhotoPaths.Contains(filePath);

        public void AddFavorite(string filePath)
        {
            if (_favoritePhotoPaths.Add(filePath)) SaveFavoritesToFile();
        }

        public void RemoveFavorite(string filePath)
        {
            if (_favoritePhotoPaths.Remove(filePath)) SaveFavoritesToFile();
        }

        public IEnumerable<string> GetFavorites()
        {
            return _favoritePhotoPaths;
        }
    }
}