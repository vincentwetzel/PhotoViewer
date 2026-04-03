using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhotoViewer.Models;
using System.Threading.Tasks;
using System;

namespace PhotoViewer.Services;

public class LocalFolderProvider : IPhotoProvider
{
    private readonly string _folderPath;

    private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

    public string SourceName => _folderPath;

    public LocalFolderProvider(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
             throw new ArgumentNullException(nameof(folderPath));
        }
        _folderPath = folderPath;
    }

    public Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync()
    {
        if (!Directory.Exists(_folderPath))
        {
            throw new DirectoryNotFoundException($"The folder '{_folderPath}' was not found. It may be on a disconnected drive or network share.");
        }

        return Task.Run(() =>
        {
            var directory = new DirectoryInfo(_folderPath);
            return directory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                .Where(file => SupportedExtensions.Contains(file.Extension.ToLowerInvariant()))
                .Select(fileInfo => new PhotoItem(
                    fileInfo.FullName,
                    fileInfo.Name,
                    fileInfo.CreationTime,
                    fileInfo.Length));
        });
    }
}