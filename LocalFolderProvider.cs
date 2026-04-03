using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhotoViewer.Models;
using System.Threading.Tasks;
using System;
using System.Windows.Media.Imaging;

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

    public async Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync()
    {
        if (!Directory.Exists(_folderPath))
        {
            throw new DirectoryNotFoundException($"The folder '{_folderPath}' was not found. It may be on a disconnected drive or network share.");
        }

        return await Task.Run(() =>
        {
            var directory = new DirectoryInfo(_folderPath);
            var items = new List<PhotoItem>();
            
            foreach (var file in directory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                .Where(file => SupportedExtensions.Contains(file.Extension.ToLowerInvariant())))
            {
                int pixelWidth = 0, pixelHeight = 0;
                try
                {
                    using var stream = file.OpenRead();
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (decoder.Frames.Count > 0)
                    {
                        pixelWidth = decoder.Frames[0].PixelWidth;
                        pixelHeight = decoder.Frames[0].PixelHeight;
                    }
                }
                catch
                {
                    // Skip if metadata cannot be read
                }
                
                items.Add(new PhotoItem(
                    file.FullName,
                    file.Name,
                    file.CreationTime,
                    file.Length,
                    pixelWidth,
                    pixelHeight));
            }
            
            return items.AsEnumerable();
        });
    }
}