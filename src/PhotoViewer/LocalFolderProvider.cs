using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhotoViewer.Models;
using System.Threading.Tasks;
using System;
using System.Threading;

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
        return await GetPhotoPathsAsync(CancellationToken.None);
    }

    /// <summary>
    /// Gets all photo paths immediately. Does NOT read file metadata (pixel dimensions) - that's deferred.
    /// This makes initial scan nearly instant even for 25,000+ files.
    /// </summary>
    public async Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_folderPath))
        {
            throw new DirectoryNotFoundException($"The folder '{_folderPath}' was not found. It may be on a disconnected drive or network share.");
        }

        return await Task.Run(() =>
        {
            var directory = new DirectoryInfo(_folderPath);
            var items = new List<PhotoItem>();

            // Enumerate files WITHOUT reading bitmap metadata - this is fast
            foreach (var file in directory.EnumerateFiles("*.*", SearchOption.AllDirectories))
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (SupportedExtensions.Contains(file.Extension.ToLowerInvariant()))
                {
                    items.Add(new PhotoItem(
                        file.FullName,
                        file.Name,
                        file.CreationTime,
                        file.Length,
                        0,  // pixelWidth - not needed for gallery display
                        0   // pixelHeight - not needed for gallery display
                    ));
                }
            }

            return items;
        }, cancellationToken);
    }

    /// <summary>
    /// Enumerates photo file paths lazily, yielding in batches to allow progressive UI updates.
    /// Returns an IEnumerable that streams paths without blocking on full enumeration.
    /// </summary>
    public IEnumerable<string> EnumeratePhotoPathsProgressively(int batchSize = 500)
    {
        if (!Directory.Exists(_folderPath))
        {
            throw new DirectoryNotFoundException($"The folder '{_folderPath}' was not found.");
        }

        var directory = new DirectoryInfo(_folderPath);
        var batch = new List<string>(batchSize);

        foreach (var file in directory.EnumerateFiles("*.*", SearchOption.AllDirectories))
        {
            if (SupportedExtensions.Contains(file.Extension.ToLowerInvariant()))
            {
                batch.Add(file.FullName);

                if (batch.Count >= batchSize)
                {
                    // Yield the batch to the caller
                    foreach (var path in batch)
                    {
                        yield return path;
                    }
                    batch.Clear();
                }
            }
        }

        // Yield remaining items
        foreach (var path in batch)
        {
            yield return path;
        }
    }
}