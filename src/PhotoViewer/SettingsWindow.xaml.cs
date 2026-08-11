using System.Windows;
using System.Windows.Controls;
using PhotoViewer.ViewModels;

namespace PhotoViewer
{
    public partial class SettingsWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public SettingsWindow(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();

            // Set the selected theme
            ThemeComboBox.SelectedItem = ThemeComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString() == viewModel.SelectedTheme);
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var theme = selectedItem.Content.ToString();
                if (theme != null)
                {
                    _viewModel.SelectedTheme = theme;
                }
            }
        }
    }
}
