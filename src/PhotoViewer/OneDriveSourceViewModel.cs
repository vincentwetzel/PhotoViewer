using PhotoViewer.Services;

namespace PhotoViewer.ViewModels
{
    public class OneDriveSourceViewModel : SourceItemViewModel
    {
        public string AccountId { get; set; } = string.Empty;

        public OneDriveSourceViewModel(OneDriveProvider provider) : base(provider)
        {
        }
    }
}