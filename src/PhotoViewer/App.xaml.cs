using System.Configuration;
using System.Data;
using System.Windows;

using PhotoViewer.ViewModels; // Add this using statement

namespace PhotoViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create the central ViewModel for the application
            var mainWindowViewModel = new MainWindowViewModel();

            // This handles opening an image file via "Open with..." or double-click
            if (e.Args.Length > 0)
            {
                string filePath = e.Args[0];
                // Directly open the image in a PhotoWindow, bypassing the main gallery window.
                mainWindowViewModel.OpenImage(filePath);
            }
            else
            {
                // If no arguments, show the main gallery window.
                var mainWindow = new MainWindow(mainWindowViewModel);
                mainWindow.Show();
            }
        }
    }
}
