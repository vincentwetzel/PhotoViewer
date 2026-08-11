using PhotoViewer.ViewModels;
using System.Windows;
using System.Windows.Controls;
using PhotoViewer.Services;
using System.Diagnostics; // Added for Debug.WriteLine

namespace PhotoViewer.Selectors
{
    public class SourceTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? FolderTemplate { get; set; }
        public DataTemplate? OneDriveTemplate { get; set; }
        public DataTemplate? GoogleDriveTemplate { get; set; }
        public DataTemplate? GalleryTemplate { get; set; }
        public DataTemplate? DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Debug.WriteLine($"SourceTemplateSelector: SelectTemplate called for item: {item?.GetType().Name ?? "null"} (DisplayName: {(item as SourceItemViewModel)?.DisplayName ?? "N/A"})");

            DataTemplate selectedTemplate;

            switch (item)
            {
                case SourceItemViewModel vm when vm.DisplayName == "Gallery":
                    Debug.WriteLine($"SourceTemplateSelector: GalleryTemplate is null? {GalleryTemplate == null}");
                    selectedTemplate = GalleryTemplate ?? DefaultTemplate!;
                    Debug.WriteLine($"SourceTemplateSelector: Item is Gallery. Selected Template: {selectedTemplate?.DataTemplateKey ?? (object)selectedTemplate?.GetType().Name ?? "null"}");
                    break;
                case SourceItemViewModel vm when vm.Provider is LocalFolderProvider:
                    Debug.WriteLine($"SourceTemplateSelector: FolderTemplate is null? {FolderTemplate == null}");
                    selectedTemplate = FolderTemplate ?? DefaultTemplate!;
                    Debug.WriteLine($"SourceTemplateSelector: Item type is LocalFolderProvider. Selected Template: {selectedTemplate?.DataTemplateKey ?? (object)selectedTemplate?.GetType().Name ?? "null"}");
                    break;
                case OneDriveSourceViewModel:
                    Debug.WriteLine($"SourceTemplateSelector: OneDriveTemplate is null? {OneDriveTemplate == null}");
                    selectedTemplate = OneDriveTemplate ?? DefaultTemplate!;
                    Debug.WriteLine($"SourceTemplateSelector: Item type is OneDriveSourceViewModel. Selected Template: {selectedTemplate?.DataTemplateKey ?? (object)selectedTemplate?.GetType().Name ?? "null"}");
                    break;
                case GoogleDriveSourceViewModel:
                    Debug.WriteLine($"SourceTemplateSelector: GoogleDriveTemplate is null? {GoogleDriveTemplate == null}");
                    selectedTemplate = GoogleDriveTemplate ?? DefaultTemplate!;
                    Debug.WriteLine($"SourceTemplateSelector: Item type is GoogleDriveSourceViewModel. Selected Template: {selectedTemplate?.DataTemplateKey ?? (object)selectedTemplate?.GetType().Name ?? "null"}");
                    break;
                default:
                    selectedTemplate = DefaultTemplate ?? base.SelectTemplate(item, container);
                    Debug.WriteLine($"SourceTemplateSelector: Item type is unknown ({item?.GetType().Name ?? "null"}). Selected Template: {selectedTemplate?.DataTemplateKey ?? (object)selectedTemplate?.GetType().Name ?? "null"}");
                    break;
            }
            return selectedTemplate;
        }
    }
}