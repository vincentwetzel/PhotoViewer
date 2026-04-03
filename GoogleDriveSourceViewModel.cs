using PhotoViewer.Services;

namespace PhotoViewer.ViewModels
{
    public class GoogleDriveSourceViewModel : SourceItemViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public GoogleDriveSourceViewModel(GoogleDriveProvider provider) : base(provider)
        {
        }
    }
}