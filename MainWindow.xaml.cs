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

                // Apply saved theme
                PhotoViewer.Services.ThemeManager.ApplyTheme(viewModel.SelectedTheme);

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

        private void ThemeComboBox_DropDownOpened(object sender, System.EventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox cb && cb.Template != null)
            {
                var popup = cb.Template.FindName("PART_Popup", cb) as System.Windows.Controls.Primitives.Popup;
                if (popup?.Child is System.Windows.Controls.Border border)
                {
                    bool isDark = DataContext is MainWindowViewModel vm &&
                        (vm.SelectedTheme == "Dark" || (vm.SelectedTheme == "System" && IsSystemDarkMode()));

                    border.Background = new System.Windows.Media.SolidColorBrush(
                        isDark ? System.Windows.Media.Color.FromRgb(43, 43, 43) : System.Windows.Media.Colors.White);
                }
            }
        }

        private bool IsSystemDarkMode()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value != null && (int)value == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
