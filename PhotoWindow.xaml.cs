﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System.Windows;
using System.Windows.Input;
using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO; // For FileSystem.DeleteFile
using System.Runtime.InteropServices;
using PhotoViewer.ViewModels;

namespace PhotoViewer
{
    /// <summary>
    /// Interaction logic for PhotoWindow.xaml
    /// </summary>
    public partial class PhotoWindow : Window
    {
        // DWM API for dark title bar
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private MatrixTransform _transform = new MatrixTransform();
        private bool _isPanning;
        private System.Windows.Point _panStartPoint;
        private string _currentFilePath = "";

        // Public properties to expose the current transform state for saving
        public double CurrentZoom => _transform.Matrix.M11; // Assuming uniform scaling
        public double CurrentPanOffsetX => _transform.Matrix.OffsetX;
        public double CurrentPanOffsetY => _transform.Matrix.OffsetY;

        public PhotoWindow()
        {
            InitializeComponent();
            PhotoImage.RenderTransform = _transform;

            // Event handlers for zoom and pan
            this.MouseWheel += PhotoWindow_MouseWheel;
            this.MouseDown += PhotoWindow_MouseDown;
            this.MouseMove += PhotoWindow_MouseMove;
            this.MouseUp += PhotoWindow_MouseUp;
            this.KeyDown += PhotoWindow_KeyDown;
            this.Loaded += PhotoWindow_Loaded;
            this.SourceInitialized += PhotoWindow_SourceInitialized;
            this.Closed += PhotoWindow_Closed;
            
            // Set focus to window for keyboard navigation
            this.Focus();
        }

        private void PhotoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load image from DataContext if available
            if (DataContext is PhotoWindowViewModel vm && !string.IsNullOrEmpty(vm.FilePath))
            {
                LoadImage(vm.FilePath);
            }

            // Apply dark mode to title bar via DWM
            ApplyDarkModeToTitleBar();
        }

        private void ApplyDarkModeToTitleBar()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                int useDark = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
            }
        }

        private void PhotoWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Apply dark mode title bar
            ApplyDarkModeToTitleBar();
            
            // Listen for system theme changes to update title bar
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
            {
                Dispatcher.Invoke(() => ApplyDarkModeToTitleBar());
            }
        }

        private void PhotoWindow_Closed(object sender, EventArgs e)
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        private void PhotoWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(this);
                this.Cursor = System.Windows.Input.Cursors.Hand;
                this.CaptureMouse();
            }
        }

        private void PhotoWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isPanning)
            {
                System.Windows.Point currentPos = e.GetPosition(this);
                var delta = currentPos - _panStartPoint;

                Matrix matrix = _transform.Matrix;
                matrix.Translate(delta.X, delta.Y);
                _transform.Matrix = matrix;

                _panStartPoint = currentPos;
            }
        }

        private void PhotoWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom factor - increase or decrease based on wheel direction
            double zoomFactor = 1.1;
            if (e.Delta < 0)
            {
                zoomFactor = 1.0 / zoomFactor;
            }

            // Get the position of the mouse relative to the image control
            System.Windows.Point mousePos = e.GetPosition(PhotoImage);

            Matrix matrix = _transform.Matrix;

            // Scale the matrix at the mouse position
            matrix.ScaleAt(zoomFactor, zoomFactor, mousePos.X, mousePos.Y);

            _transform.Matrix = matrix;
        }

        private void PhotoWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                this.ReleaseMouseCapture();
            }
        }

        private void PhotoWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to move this file to the Recycle Bin?\n\nFile: {_currentFilePath}",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        FileSystem.DeleteFile(_currentFilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to delete the file.\n\nReason: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (e.Key == Key.Left || e.Key == Key.Right)
            {
                NavigateImage(e.Key == Key.Right);
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateImage(navigateNext: false);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateImage(navigateNext: true);
        }

        private void NavigateImage(bool navigateNext)
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath)) return;

            var directory = Path.GetDirectoryName(_currentFilePath);
            if (directory == null) return;

            var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp" };
            var files = Directory.EnumerateFiles(directory)
                                 .Where(f => supportedExtensions.Contains(Path.GetExtension(f)))
                                 .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                 .ToList();

            if (files.Count <= 1) return;

            int currentIndex = files.FindIndex(f => string.Equals(f, _currentFilePath, StringComparison.OrdinalIgnoreCase));
            if (currentIndex == -1) return;

            int nextIndex = navigateNext ? currentIndex + 1 : currentIndex - 1;

            // Loop around if reaching the end or beginning
            if (nextIndex >= files.Count)
            {
                nextIndex = 0;
            }
            if (nextIndex < 0)
            {
                nextIndex = files.Count - 1;
            }

            // Load the new image, which will replace the content of the current window
            LoadImage(files[nextIndex]);
        }

        /// <summary>
        /// Loads an image from the specified file path.
        /// </summary>
        public bool LoadImage(string filePath)
        {
            try
            {
                _currentFilePath = filePath;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                // CacheOption.OnLoad ensures the file is not locked after loading.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                PhotoImage.Source = bitmap;
                this.Title = System.IO.Path.GetFileName(filePath);
                return true;
            }
            catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is NotSupportedException)
            {
                System.Windows.MessageBox.Show(
                    $"Could not load the image file.\n\nFile: {filePath}\nReason: {ex.Message}",
                    "Error Loading Image",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Loads an image and applies a previously saved transform state.
        /// </summary>
        /// <returns>True if the image was loaded successfully, otherwise false.</returns>
        public bool LoadImage(string filePath, double zoom, double panX, double panY)
        {
            if (!LoadImage(filePath)) return false;

            _transform.Matrix = new Matrix(zoom, 0, 0, zoom, panX, panY);
            return true;
        }
    }
}