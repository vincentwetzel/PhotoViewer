using System.ComponentModel;
using PhotoViewer.Models;
using PhotoViewer.ViewModels;

namespace PhotoViewer.Services;

public static class PhotoSortDescriptor
{
    public static string GetPropertyName(string? sortOption) => sortOption switch
    {
        "Date taken" => $"{nameof(PhotoItemViewModel.Photo)}.{nameof(PhotoItem.DateTaken)}",
        "Date created" => $"{nameof(PhotoItemViewModel.Photo)}.{nameof(PhotoItem.CreationDate)}",
        "Date modified" => $"{nameof(PhotoItemViewModel.Photo)}.{nameof(PhotoItem.DateModified)}",
        "File size" => $"{nameof(PhotoItemViewModel.Photo)}.{nameof(PhotoItem.FileSizeInBytes)}",
        _ => $"{nameof(PhotoItemViewModel.Photo)}.{nameof(PhotoItem.FileName)}"
    };

    public static ListSortDirection GetDirection(string? sortDirection) =>
        string.Equals(sortDirection, "Descending", StringComparison.OrdinalIgnoreCase)
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

    public static DateTime GetGroupingDate(PhotoItem photo, string? sortOption) => sortOption switch
    {
        "Date created" => photo.CreationDate,
        "Date modified" => photo.DateModified,
        _ => photo.DateTaken
    };
}
