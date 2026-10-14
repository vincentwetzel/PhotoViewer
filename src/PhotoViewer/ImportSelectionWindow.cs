using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PhotoViewer;

internal sealed class ImportSelectionItem : INotifyPropertyChanged
{
    public string FilePath { get; }
    public string DisplayName => Path.GetFileName(FilePath);
    public long FileSize { get; }
    public string AccessibilityName => FileSize > 0
        ? $"{DisplayName}, {FileSize:N0} bytes"
        : DisplayName;

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); }
    }

    public ImportSelectionItem(string filePath)
    {
        FilePath = filePath;
        try { FileSize = new FileInfo(filePath).Length; } catch { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class ImportSelectionWindow : Window
{
    private readonly ObservableCollection<ImportSelectionItem> _items;
    public IReadOnlyList<string> SelectedPaths => _items.Where(item => item.IsSelected).Select(item => item.FilePath).ToList();

    public ImportSelectionWindow(IEnumerable<string> paths)
    {
        Title = "Select items to import";
        AutomationProperties.SetName(this, "PhotoViewer import selection");
        Width = 620;
        Height = 520;
        MinWidth = 480;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (System.Windows.Media.Brush)Application.Current.Resources["WindowBackground"];

        _items = new ObservableCollection<ImportSelectionItem>(paths.Select(path => new ImportSelectionItem(path)));
        var list = new ListBox { ItemsSource = _items, Margin = new Thickness(0, 10, 0, 10) };
        AutomationProperties.SetName(list, "Items available for import");
        list.ItemTemplate = CreateItemTemplate();

        var selectAll = new Button { Content = "Select all", Padding = new Thickness(10, 4, 10, 4) };
        AutomationProperties.SetName(selectAll, "Select all import items");
        selectAll.Click += (_, _) => SetSelection(true);
        var selectNone = new Button { Content = "Select none", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetName(selectNone, "Clear import item selection");
        selectNone.Click += (_, _) => SetSelection(false);
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetName(cancel, "Cancel import");
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        var import = new Button { Content = "Continue", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
        AutomationProperties.SetName(import, "Continue with selected import items");
        import.Click += (_, _) =>
        {
            if (SelectedPaths.Count == 0)
            {
                MessageBox.Show(this, "Select at least one item to import.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
            Close();
        };

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(selectAll);
        actions.Children.Add(selectNone);
        actions.Children.Add(cancel);
        actions.Children.Add(import);

        var panel = new DockPanel { Margin = new Thickness(18) };
        DockPanel.SetDock(actions, Dock.Bottom);
        panel.Children.Add(actions);
        panel.Children.Add(list);
        Content = panel;
    }

    private void SetSelection(bool selected)
    {
        foreach (var item in _items) item.IsSelected = selected;
    }

    private static DataTemplate CreateItemTemplate()
    {
        var template = new DataTemplate(typeof(ImportSelectionItem));
        var factory = new FrameworkElementFactory(typeof(CheckBox));
        factory.SetBinding(ContentControl.ContentProperty, new System.Windows.Data.Binding(nameof(ImportSelectionItem.DisplayName)));
        factory.SetBinding(AutomationProperties.NameProperty, new System.Windows.Data.Binding(nameof(ImportSelectionItem.AccessibilityName)));
        factory.SetBinding(FrameworkElement.ToolTipProperty, new System.Windows.Data.Binding(nameof(ImportSelectionItem.FilePath)));
        factory.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding(nameof(ImportSelectionItem.IsSelected))
        {
            Mode = System.Windows.Data.BindingMode.TwoWay
        });
        factory.SetValue(Control.PaddingProperty, new Thickness(8, 6, 8, 6));
        template.VisualTree = factory;
        return template;
    }
}
