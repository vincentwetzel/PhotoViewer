using PhotoViewer.ViewModels;
using System.Globalization;

namespace PhotoViewer.Services;

public static class PhotoQueryMatcher
{
    public static bool Matches(PhotoItemViewModel item, string mediaType, string? searchText)
    {
        var mediaMatches = mediaType switch
        {
            "Photos" => !item.Photo.IsVideo,
            "Videos" => item.Photo.IsVideo,
            _ => true
        };
        if (!mediaMatches) return false;
        if (string.IsNullOrWhiteSpace(searchText)) return true;

        var query = searchText.Trim();
        return item.Photo.FileName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.Photo.FilePath.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.Photo.CreationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                .Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
