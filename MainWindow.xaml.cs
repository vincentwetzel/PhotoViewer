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
                var gallerySource = viewModel.CollectionSources.FirstOrDefault(s => s.DisplayName == "Gallery");
                if (gallerySource != null)
                {
                    viewModel.SelectedSource = gallerySource;
                }

                // Listen for system theme changes
                Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == Microsoft.Win32.UserPreferenceCategory.General && DataContext is MainWindowViewModel vm)
            {
                // If theme is set to "System", re-apply it
                if (vm.SelectedTheme == "System")
                {
                    Dispatcher.Invoke(() => vm.ApplyTheme());
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
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

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            object? sourceToRemove = null;

            // Check if the TreeView has a selected item
            if (SourceTreeView.SelectedItem is not null)
            {
                sourceToRemove = SourceTreeView.SelectedItem;
            }

            if (sourceToRemove is not PhotoViewer.ViewModels.SourceItemViewModel and not PhotoViewer.ViewModels.FolderSourceViewModel)
                return;

            if (this.DataContext is PhotoViewer.ViewModels.MainWindowViewModel viewModel &&
                viewModel.RemoveSourceCommand.CanExecute(sourceToRemove))
            {
                viewModel.RemoveSourceCommand.Execute(sourceToRemove);
            }
        }

        /// <summary>
        /// Handles chevron click to expand/collapse folder trees.
        /// Only the chevron triggers expand/collapse; clicking the row selects it.
        /// </summary>
        private void Chevron_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;

            // Find the parent TreeViewItem
            var depObj = sender as System.Windows.DependencyObject;
            TreeViewItem? treeViewItem = null;
            while (depObj != null)
            {
                if (depObj is TreeViewItem tvi)
                {
                    treeViewItem = tvi;
                    break;
                }
                depObj = System.Windows.Media.VisualTreeHelper.GetParent(depObj);
            }

            if (treeViewItem != null)
            {
                treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
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

        /// <summary>
        /// Handles TreeView selection. Determines what was clicked and sets the MainWindowViewModel.SelectedSource accordingly.
        /// </summary>
        private void SourceTreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = e.NewValue;
            if (selectedItem == null) return;

            if (selectedItem is PhotoViewer.Models.FolderNode node)
            {
                if (node.RootSource is PhotoViewer.ViewModels.FolderSourceViewModel folderSource)
                {
                    folderSource.SelectedItem = node;
                    if (DataContext is PhotoViewer.ViewModels.MainWindowViewModel vm)
                    {
                        if (vm.SelectedSource != folderSource)
                        {
                            // First click on this folder tree (or switching from another source)
                            vm.SelectedSource = folderSource;
                        }
                        else
                        {
                            // Same folder tree but different subfolder — force reload
                            vm.ReloadCurrentSource();
                        }
                    }
                }
            }
            else if (selectedItem is PhotoViewer.ViewModels.FolderSourceViewModel rootFolderSource)
            {
                rootFolderSource.SelectedItem = null;
                if (DataContext is PhotoViewer.ViewModels.MainWindowViewModel vm2)
                {
                    if (vm2.SelectedSource != rootFolderSource)
                    {
                        vm2.SelectedSource = rootFolderSource;
                    }
                    else
                    {
                        // Same folder tree but now the root is selected — force reload with all photos
                        vm2.ReloadCurrentSource();
                    }
                }
            }
            else if (selectedItem is PhotoViewer.ViewModels.SourceItemViewModel cloudSource)
            {
                if (DataContext is PhotoViewer.ViewModels.MainWindowViewModel vm3)
                    vm3.SelectedSource = cloudSource;
            }
        }

        private void ThemeComboBox_DropDownOpened(object sender, System.EventArgs e)
        {
            // The ComboBox popup background is handled by DynamicResource - no need for hardcoded colors here.
            // This handler is kept for future customization if needed.
        }
    }
}
