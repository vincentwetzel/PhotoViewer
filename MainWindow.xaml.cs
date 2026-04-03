using System.Windows;
using System.Windows.Controls;
using PhotoViewer.Services;
using PhotoViewer.ViewModels;

namespace PhotoViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            // Apply theme BEFORE InitializeComponent so resources are available during XAML parsing
            PhotoViewer.Services.ThemeManager.ApplyTheme(viewModel.SelectedTheme);
            
            InitializeComponent();
            DataContext = viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.InitializeAsync();

                // Select Gallery by default
                var gallerySource = viewModel.Sources.FirstOrDefault(s => s.DisplayName == "Gallery");
                if (gallerySource != null)
                {
                    viewModel.SelectedSource = gallerySource;
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == System.Windows.WindowState.Maximized
                ? System.Windows.WindowState.Normal
                : System.Windows.WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SourceListBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Only show context menu if an item is selected
            if (SourceListBox.SelectedItem == null)
            {
                e.Handled = true;
                return;
            }

            // Enable/disable Remove menu item based on selected source type
            if (SourceListBox.SelectedItem is SourceItemViewModel source &&
                SourceListBox.ContextMenu is ContextMenu contextMenu &&
                contextMenu.Items[0] is MenuItem removeMenuItem)
            {
                removeMenuItem.IsEnabled = source.Provider is LocalFolderProvider or OneDriveProvider or GoogleDriveProvider;
            }
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SourceListBox.SelectedItem is SourceItemViewModel selectedSource &&
                SourceListBox.DataContext is MainWindowViewModel viewModel &&
                viewModel.RemoveSourceCommand.CanExecute(selectedSource))
            {
                viewModel.RemoveSourceCommand.Execute(selectedSource);
            }
        }

        private void PhotoListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Only open if an item was added to the selection (not removed)
            if (sender is System.Windows.Controls.ListBox listBox &&
                e.AddedItems.Count > 0 &&
                e.AddedItems[0] is PhotoItemViewModel photoVm &&
                listBox.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.OpenImage(photoVm.Photo.FilePath);
            }
        }
    }
}
