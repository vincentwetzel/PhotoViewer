using PhotoViewer.ViewModels;

namespace PhotoViewer.Services;

public static class PhotoSelectionService
{
    public static int Count(IEnumerable<PhotoItemViewModel> photos) =>
        photos.Count(photo => photo.IsSelected);

    public static void SelectAll(IEnumerable<PhotoItemViewModel> photos)
    {
        foreach (var photo in photos)
            photo.IsSelected = true;
    }

    public static void Clear(IEnumerable<PhotoItemViewModel> photos)
    {
        foreach (var photo in photos)
            photo.IsSelected = false;
    }
}
