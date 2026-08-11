using PhotoViewer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.Services;

/// <summary>
/// A photo provider that aggregates photos from all other sources.
/// </summary>
public class GalleryProvider : IPhotoProvider
{
    private readonly IEnumerable<IPhotoProvider> _providers;

    public string SourceName => "Gallery";

    public GalleryProvider(IEnumerable<IPhotoProvider> providers)
    {
        _providers = providers;
    }

    public Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync()
    {
        return GetPhotoPathsAsync(CancellationToken.None);
    }

    public async Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync(CancellationToken cancellationToken)
    {
        var allPhotos = new List<PhotoItem>();

        foreach (var provider in _providers)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var photos = await provider.GetPhotoPathsAsync(cancellationToken);
                allPhotos.AddRange(photos);
            }
            catch
            {
                // Skip providers that fail to load
            }
        }

        // Remove duplicates based on file path
        return allPhotos.GroupBy(p => p.FilePath).Select(g => g.First());
    }
}
