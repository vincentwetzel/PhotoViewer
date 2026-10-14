using System.Windows;
using System.Windows.Controls;

namespace PhotoViewer.Selectors;

public class SourceTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? GalleryTemplate { get; set; }
    public DataTemplate? DefaultTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is PhotoViewer.ViewModels.SourceItemViewModel source && source.DisplayName == "Gallery")
            return GalleryTemplate ?? DefaultTemplate ?? base.SelectTemplate(item, container);
        if (item is PhotoViewer.ViewModels.SourceItemViewModel { Provider: PhotoViewer.Services.LocalFolderProvider })
            return FolderTemplate ?? DefaultTemplate ?? base.SelectTemplate(item, container);
        return DefaultTemplate ?? base.SelectTemplate(item, container);
    }
}
