using PhotoViewer.Models;
using System.IO;
using System.Text.Json;

namespace PhotoViewer.Services
{
    /// <summary>
    /// Service responsible for saving and loading workspace layouts.
    /// </summary>
    public class LayoutService
    {
        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true // For human-readable JSON files
        };

        public void SaveLayout(WindowLayout layout, string filePath)
        {
            string jsonString = JsonSerializer.Serialize(layout, _options);
            File.WriteAllText(filePath, jsonString);
        }
    }
}