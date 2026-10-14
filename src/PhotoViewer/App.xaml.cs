using System.Configuration;
using System.Data;
using System.Windows;

using PhotoViewer.ViewModels; // Add this using statement
using PhotoViewer.Services;

namespace PhotoViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private readonly DiagnosticsService _diagnostics = new();

        public App()
        {
            DispatcherUnhandledException += (_, args) =>
            {
                _diagnostics.LogUnhandledException(args.Exception, "DispatcherUnhandledException");
                args.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                    _diagnostics.LogUnhandledException(exception, "AppDomain.UnhandledException");
            };
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create the central ViewModel for the application
            var mainWindowViewModel = new MainWindowViewModel();

            // This handles opening an image file via "Open with..." or double-click
            var fileArguments = e.Args
                .Where(File.Exists)
                .Where(MediaFileTypes.IsSupported)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (fileArguments.Length > 0)
            {
                // Open every file passed by a shell association or drag/drop invocation.
                foreach (var filePath in fileArguments)
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
