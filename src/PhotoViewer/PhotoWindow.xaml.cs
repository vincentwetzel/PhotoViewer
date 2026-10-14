﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.Windows.Ink;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.VisualBasic.FileIO; // For FileSystem.DeleteFile
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PhotoViewer.ViewModels;
using PhotoViewer.Services;

namespace PhotoViewer
{
    /// <summary>
    /// Interaction logic for PhotoWindow.xaml
    /// </summary>
    public partial class PhotoWindow : Window
    {
        // DWM API for dark title bar
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        
        // Windows messages for intercepting maximize
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_RESTORE = 0xF120;
        private const int SC_CLOSE = 0xF060;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private MatrixTransform _transform = new MatrixTransform();
        private bool _isPanning;
        private System.Windows.Point _panStartPoint;
        private string _currentFilePath = "";
        private bool _isMaximized = false;
        private Rect _restoreBounds;
        private readonly List<NavigationItem> _navigationItems = new();
        private bool _suppressFilmstripSelection;
        private bool _suppressVideoSeek;
        private bool _videoIsPlaying;
        private readonly DispatcherTimer _videoProgressTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
        private EditSession? _editSession;
        private readonly IImageEditingService _editingService = new ImageEditingService();
        private readonly ScaleTransform _editFlipTransform = new();
        private BitmapSource? _originalBitmap;
        private bool _suppressAdjustmentEvents;
        private bool _isSavingCopy;

        public IReadOnlyList<NavigationItem> NavigationItems => _navigationItems;
        private bool IsCurrentVideo => MediaFileTypes.IsVideo(_currentFilePath);
        private bool CanEditCurrent => File.Exists(_currentFilePath) && !IsCurrentVideo;

        public sealed class NavigationItem
        {
            public string FilePath { get; }
            public bool IsVideo { get; }

            public NavigationItem(string filePath)
            {
                FilePath = filePath;
                var extension = Path.GetExtension(filePath);
                IsVideo = MediaFileTypes.IsVideo(filePath);
            }
        }

        // Public properties to expose the current transform state for saving
        public double CurrentZoom => _transform.Matrix.M11; // Assuming uniform scaling
        public double CurrentPanOffsetX => _transform.Matrix.OffsetX;
        public double CurrentPanOffsetY => _transform.Matrix.OffsetY;

        public PhotoWindow()
        {
            InitializeComponent();
            _videoProgressTimer.Tick += (_, _) => VideoPlayer_PositionChanged(VideoPlayer, new RoutedEventArgs());
            PhotoImage.RenderTransform = _transform;
            PhotoImage.LayoutTransform = _editFlipTransform;

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
                // Read the actual theme setting from app settings
                var settingsService = new PhotoViewer.Services.SettingsService();
                var settings = settingsService.LoadSettings();
                
                bool useDark = settings.Theme switch
                {
                    "Light" => false,
                    "Dark" => true,
                    "System" => PhotoViewer.Services.ThemeManager.IsSystemDarkMode(),
                    _ => PhotoViewer.Services.ThemeManager.IsSystemDarkMode()
                };
                
                int useDarkValue = useDark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkValue, sizeof(int));
            }
        }

        private void PhotoWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Apply dark mode title bar
            ApplyDarkModeToTitleBar();

            // Listen for system theme changes to update title bar
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            // Hook into window messages to intercept maximize/restore
            var source = System.Windows.Interop.HwndSource.FromHwnd(
                new System.Windows.Interop.WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xFFF0;
                
                if (command == SC_MAXIMIZE)
                {
                    // Intercept maximize - save bounds and use working area
                    _restoreBounds = new Rect(this.Left, this.Top, this.Width, this.Height);
                    var workingArea = System.Windows.SystemParameters.WorkArea;
                    this.Left = workingArea.Left;
                    this.Top = workingArea.Top;
                    this.Width = workingArea.Width;
                    this.Height = workingArea.Height;
                    _isMaximized = true;
                    handled = true;
                    return IntPtr.Zero;
                }
                else if (command == SC_RESTORE && _isMaximized)
                {
                    // Intercept restore from maximized
                    this.Left = _restoreBounds.Left;
                    this.Top = _restoreBounds.Top;
                    this.Width = _restoreBounds.Width;
                    this.Height = _restoreBounds.Height;
                    _isMaximized = false;
                    handled = true;
                    return IntPtr.Zero;
                }
            }
            return IntPtr.Zero;
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
                ConstrainPan(ref matrix);
                _transform.Matrix = matrix;

                _panStartPoint = currentPos;
            }
        }

        private void PhotoWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var settings = new PhotoViewer.Services.SettingsService().LoadSettings();
            if (!settings.MouseWheelZoom)
                return;

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
            ConstrainPan(ref matrix);

            _transform.Matrix = matrix;
            UpdateZoomText();
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
            if (e.Key == Key.V)
            {
                FavoriteButton_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F)
            {
                ResetZoom();
                e.Handled = true;
            }
            else if (e.Key == Key.D1 || e.Key == Key.NumPad1)
            {
                SetZoom(1.0);
                e.Handled = true;
            }
            else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
            {
                ResetZoom();
                e.Handled = true;
            }
            else if (e.Key == Key.I)
            {
                InfoPanel.Visibility = InfoPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                UndoEditMenuItem_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y)
            {
                RedoEditMenuItem_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Space && VideoPlayer.Visibility == Visibility.Visible)
            {
                VideoPlayButton_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                var settings = new PhotoViewer.Services.SettingsService().LoadSettings();
                var result = settings.ConfirmDelete
                    ? System.Windows.MessageBox.Show(
                        $"Are you sure you want to move this file to the Recycle Bin?\n\nFile: {_currentFilePath}",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning)
                    : MessageBoxResult.Yes;

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

        public void SetNavigationItems(IEnumerable<string> filePaths)
        {
            _navigationItems.Clear();
            foreach (var path in filePaths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
                _navigationItems.Add(new NavigationItem(path));
            Filmstrip.ItemsSource = null;
            Filmstrip.ItemsSource = _navigationItems;
            SelectCurrentFilmstripItem();
        }

        private void Filmstrip_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_suppressFilmstripSelection || e.AddedItems.Count == 0 || e.AddedItems[0] is not NavigationItem item)
                return;
            if (!File.Exists(item.FilePath))
            {
                MessageBox.Show("This related item is no longer available.", "Photo unavailable", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            LoadImage(item.FilePath);
        }

        private void SelectCurrentFilmstripItem()
        {
            if (Filmstrip == null) return;
            _suppressFilmstripSelection = true;
            Filmstrip.SelectedItem = _navigationItems.FirstOrDefault(item =>
                string.Equals(item.FilePath, _currentFilePath, StringComparison.OrdinalIgnoreCase));
            _suppressFilmstripSelection = false;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateImage(navigateNext: true);
        }

        private void FitButton_Click(object sender, RoutedEventArgs e) => ResetZoom();

        private void VideoPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.Visibility != Visibility.Visible) return;
            if (_videoIsPlaying)
            {
                VideoPlayer.Pause();
                _videoIsPlaying = false;
                VideoPlayButton.Content = "Play";
            }
            else
            {
                VideoPlayer.Play();
                _videoIsPlaying = true;
                VideoPlayButton.Content = "Pause";
            }
        }

        private void VideoProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_suppressVideoSeek && VideoPlayer.NaturalDuration.HasTimeSpan)
                VideoPlayer.Position = TimeSpan.FromSeconds(e.NewValue);
        }

        private void VideoVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoPlayer.Volume = e.NewValue;
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            VideoControls.Visibility = Visibility.Visible;
            VideoProgress.Maximum = VideoPlayer.NaturalDuration.HasTimeSpan
                ? VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds : 1;
            VideoStatusText.Text = "Ready";
            VideoPlayer.Play();
            _videoIsPlaying = true;
            _videoProgressTimer.Start();
            VideoPlayButton.Content = "Pause";
        }

        private void VideoPlayer_PositionChanged(object sender, RoutedEventArgs e)
        {
            if (!VideoPlayer.NaturalDuration.HasTimeSpan) return;
            _suppressVideoSeek = true;
            VideoProgress.Value = VideoPlayer.Position.TotalSeconds;
            _suppressVideoSeek = false;
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            _videoProgressTimer.Stop();
            _videoIsPlaying = false;
            VideoPlayButton.Content = "Play";
            VideoStatusText.Text = "Ended";
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            VideoPlayer.Stop();
            _videoProgressTimer.Stop();
            _videoIsPlaying = false;
            VideoControls.Visibility = Visibility.Visible;
            VideoStatusText.Text = "This video codec is not supported.";
            VideoPlayButton.IsEnabled = false;
        }

        private void ActualSizeButton_Click(object sender, RoutedEventArgs e) => SetZoom(1.0);

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e) => ResetZoom();

        private void ZoomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { Tag: string tag } && double.TryParse(tag, System.Globalization.CultureInfo.InvariantCulture, out var zoom))
                SetZoom(zoom);
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            InfoPanel.Visibility = InfoPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AdjustmentsButton_Click(object sender, RoutedEventArgs e)
        {
            AdjustmentsPanel.Visibility = AdjustmentsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateEditAvailability();
        }

        private void AdjustmentSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressAdjustmentEvents || _editSession == null || IsCurrentVideo || !AdjustmentsPanel.IsVisible) return;
            _editSession.SetAdjustments(new ImageAdjustments(
                ExposureSlider.Value, BrightnessSlider.Value, ContrastSlider.Value,
                HighlightsSlider.Value, ShadowsSlider.Value, SaturationSlider.Value,
                TemperatureSlider.Value, TintSlider.Value, SharpnessSlider.Value));
            ApplyAdjustmentPreview();
            UpdateEditStatus();
        }

        private void ResetAdjustmentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editSession == null || IsCurrentVideo) return;
            ExposureSlider.Value = 0;
            BrightnessSlider.Value = 0;
            ContrastSlider.Value = 0;
            HighlightsSlider.Value = 0;
            ShadowsSlider.Value = 0;
            SaturationSlider.Value = 0;
            TemperatureSlider.Value = 0;
            TintSlider.Value = 0;
            SharpnessSlider.Value = 0;
            _editSession.SetAdjustments(ImageAdjustments.Default);
            ApplyAdjustmentPreview();
            UpdateEditStatus();
        }

        private void ApplyAdjustmentPreview()
        {
            if (_editSession == null || _originalBitmap == null) return;
            if (_editSession.Adjustments.IsDefault && _editSession.Filter == ImageFilter.None &&
                _editSession.BackgroundFill == ImageBackgroundFill.None)
            {
                PhotoImage.Source = _originalBitmap;
                return;
            }

            var converted = new FormatConvertedBitmap(_originalBitmap, PixelFormats.Bgra32, null, 0);
            converted.Freeze();
            var pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4];
            converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
            ImageAdjustmentProcessor.ApplyBgra32(pixels, _editSession.Adjustments);
            ImageFilterProcessor.ApplyBgra32(pixels, _editSession.Filter);
            ImageBackgroundProcessor.ApplyBgra32(pixels, _editSession.BackgroundFill, converted.PixelWidth, converted.PixelHeight);
            var adjusted = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, converted.DpiX, converted.DpiY,
                PixelFormats.Bgra32, null, pixels, converted.PixelWidth * 4);
            adjusted.Freeze();
            PhotoImage.Source = adjusted;
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath)) return;
            var favorites = new PhotoViewer.Services.FavoritesService();
            if (favorites.IsFavorite(_currentFilePath))
            {
                favorites.RemoveFavorite(_currentFilePath);
                FavoriteButton.Content = "Favorite";
            }
            else
            {
                favorites.AddFavorite(_currentFilePath);
                FavoriteButton.Content = "Unfavorite";
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath)) return;
            var paths = new StringCollection { _currentFilePath };
            Clipboard.SetFileDropList(paths);
        }

        private async void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_isSavingCopy || !CanEditCurrent) return;
            var dialog = new SaveFileDialog
            {
                Title = "Save a copy",
                FileName = Path.GetFileName(_currentFilePath),
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*"
            };
            if (dialog.ShowDialog() != true) return;
            _isSavingCopy = true;
            Mouse.OverrideCursor = Cursors.Wait;
            SaveAsMenuItem.IsEnabled = false;
            try
            {
                await _editingService.SaveEditedCopyAsync(
                    _currentFilePath, dialog.FileName, _editSession?.RotationDegrees ?? 0,
                    _editSession?.FlipHorizontal ?? false, _editSession?.CropAspectRatio,
                    _editSession?.StraightenDegrees ?? 0, _editSession?.Adjustments ?? ImageAdjustments.Default,
                    _editSession?.Filter ?? ImageFilter.None, _editSession?.MarkupStrokes,
                    _editSession?.BackgroundFill ?? ImageBackgroundFill.None);
                _editSession?.Reset();
                UpdateEditStatus();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Saving the copy was canceled.", "Save as", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save a copy: {ex.Message}", "Save as", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isSavingCopy = false;
                Mouse.OverrideCursor = null;
                UpdateEditAvailability();
            }
        }

        private void PrintMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath)) return;
            Process.Start(new ProcessStartInfo { FileName = _currentFilePath, Verb = "print", UseShellExecute = true });
        }

        private void RotateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo) return;
            _editSession?.RotateClockwise();
            var matrix = _transform.Matrix;
            matrix.RotateAt(90, PhotoImage.ActualWidth / 2, PhotoImage.ActualHeight / 2);
            ConstrainPan(ref matrix);
            _transform.Matrix = matrix;
            UpdateEditStatus();
        }

        private void FlipHorizontalMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo) return;
            _editSession?.ToggleHorizontalFlip();
            _editFlipTransform.ScaleX = _editSession?.FlipHorizontal == true ? -1 : 1;
            UpdateEditStatus();
        }

        private void CropFourThreeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo) return;
            var current = _editSession?.CropAspectRatio;
            _editSession?.SetCropAspectRatio(current.HasValue ? null : 4.0 / 3.0);
            UpdateEditStatus();
        }

        private void CropPresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo || sender is not MenuItem menuItem ||
                !double.TryParse(menuItem.Tag?.ToString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var aspectRatio))
                return;

            var current = _editSession?.CropAspectRatio;
            _editSession?.SetCropAspectRatio(current.HasValue && Math.Abs(current.Value - aspectRatio) < 0.0001
                ? null
                : aspectRatio);
            UpdateEditStatus();
        }

        private void StraightenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo || sender is not MenuItem menuItem ||
                !double.TryParse(menuItem.Tag?.ToString(), out var degrees)) return;
            _editSession?.AdjustStraighten(degrees);
            ApplyEditPreview();
            UpdateEditStatus();
        }

        private void StraightenResetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo || _editSession == null || Math.Abs(_editSession.StraightenDegrees) < 0.0001) return;
            _editSession.AdjustStraighten(-_editSession.StraightenDegrees);
            ApplyEditPreview();
            UpdateEditStatus();
        }

        private void FilterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo || _editSession == null || sender is not MenuItem item ||
                !Enum.TryParse<ImageFilter>(item.Tag?.ToString(), out var filter)) return;
            _editSession.SetFilter(filter);
            ApplyAdjustmentPreview();
            UpdateEditStatus();
        }

        private void AnnotationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEditCurrent) return;
            AnnotationTextBox.Text = _editSession?.Annotation ?? string.Empty;
            AnnotationPanel.Visibility = AnnotationPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ApplyAnnotationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editSession == null || !CanEditCurrent) return;
            _editSession.SetAnnotation(AnnotationTextBox.Text);
            AnnotationPanel.Visibility = Visibility.Collapsed;
            UpdateEditStatus();
        }

        private void ClearAnnotationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editSession == null || !CanEditCurrent) return;
            _editSession.SetAnnotation(string.Empty);
            AnnotationTextBox.Clear();
            AnnotationPanel.Visibility = Visibility.Collapsed;
            UpdateEditStatus();
        }

        private void MarkupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEditCurrent) return;
            MarkupCanvas.Visibility = MarkupCanvas.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            MarkupCanvas.IsHitTestVisible = MarkupCanvas.Visibility == Visibility.Visible;
            MarkupCanvas.EditingMode = MarkupCanvas.IsHitTestVisible
                ? InkCanvasEditingMode.Ink : InkCanvasEditingMode.None;
            MarkupCanvas.DefaultDrawingAttributes.Color = Colors.Yellow;
            MarkupCanvas.DefaultDrawingAttributes.Width = 3;
            MarkupCanvas.DefaultDrawingAttributes.Height = 3;
        }

        private void MarkupCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            var points = e.Stroke.StylusPoints
                .Select(point => new MarkupPoint(
                    MarkupCanvas.ActualWidth > 0 ? point.X / MarkupCanvas.ActualWidth : 0,
                    MarkupCanvas.ActualHeight > 0 ? point.Y / MarkupCanvas.ActualHeight : 0))
                .ToArray();
            var color = MarkupCanvas.DefaultDrawingAttributes.Color;
            _editSession?.AddInkStroke(new MarkupStroke(points, color.R, color.G, color.B,
                MarkupCanvas.DefaultDrawingAttributes.Width));
            UpdateEditStatus();
        }

        private void SyncMarkupCanvas()
        {
            MarkupCanvas.Strokes.Clear();
            if (_editSession == null || MarkupCanvas.ActualWidth <= 0 || MarkupCanvas.ActualHeight <= 0) return;
            foreach (var markup in _editSession.MarkupStrokes.Where(stroke => stroke.Points.Count > 1))
            {
                var points = new StylusPointCollection(markup.Points.Select(point =>
                    new StylusPoint(point.X * MarkupCanvas.ActualWidth, point.Y * MarkupCanvas.ActualHeight)));
                MarkupCanvas.Strokes.Add(new Stroke(points)
                {
                    DrawingAttributes = new DrawingAttributes
                    {
                        Color = Color.FromRgb(markup.Red, markup.Green, markup.Blue),
                        Width = markup.Width,
                        Height = markup.Width
                    }
                });
            }
        }

        private void ClearMarkupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_editSession == null || !CanEditCurrent) return;
            MarkupCanvas.Strokes.Clear();
            _editSession.ClearInk();
            SyncMarkupCanvas();
            UpdateEditStatus();
        }

        private void BackgroundMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo || _editSession == null || sender is not MenuItem item ||
                !Enum.TryParse<ImageBackgroundFill>(item.Tag?.ToString(), out var fill)) return;
            _editSession.SetBackgroundFill(fill);
            ApplyAdjustmentPreview();
            UpdateEditStatus();
        }

        private void UndoEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo) return;
            if (_editSession?.Undo() != true) return;
            LoadImage(_currentFilePath, resetEditSession: false);
            ApplyEditPreview();
            SyncMarkupCanvas();
            UpdateEditStatus();
        }

        private void RedoEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo || _editSession?.Redo() != true) return;
            LoadImage(_currentFilePath, resetEditSession: false);
            ApplyEditPreview();
            SyncMarkupCanvas();
            UpdateEditStatus();
        }

        private void ApplyEditPreview()
        {
            if (_editSession == null) return;
            ApplyAdjustmentPreview();
            _editFlipTransform.ScaleX = _editSession.FlipHorizontal ? -1 : 1;
            var matrix = _transform.Matrix;
            for (var i = 0; i < _editSession.RotationDegrees / 90; i++)
                matrix.RotateAt(90, PhotoImage.ActualWidth / 2, PhotoImage.ActualHeight / 2);
            if (Math.Abs(_editSession.StraightenDegrees) > 0.0001)
                matrix.RotateAt(_editSession.StraightenDegrees, PhotoImage.ActualWidth / 2, PhotoImage.ActualHeight / 2);
            ConstrainPan(ref matrix);
            _transform.Matrix = matrix;
        }

        private void ResetEditsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo) return;
            if (_editSession == null || !_editSession.IsDirty) return;
            _editSession.Reset();
            LoadImage(_currentFilePath);
            UpdateEditStatus();
        }

        private void CancelEditsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentVideo || _editSession == null || !_editSession.IsDirty) return;
            if (MessageBox.Show("Discard the edits made in this viewer?", "Cancel edits",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _editSession.Reset();
            LoadImage(_currentFilePath);
            UpdateEditStatus();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath)) return;
            var settings = new PhotoViewer.Services.SettingsService().LoadSettings();
            if (settings.ConfirmDelete && MessageBox.Show(
                    $"Move this file to the Recycle Bin?\n\n{Path.GetFileName(_currentFilePath)}",
                    "Delete photo", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            try
            {
                FileSystem.DeleteFile(_currentFilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete the file: {ex.Message}", "Delete photo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenWithMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"shell32.dll,OpenAs_RunDLL \"{_currentFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        private void OpenContainingFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{_currentFilePath}\"",
                UseShellExecute = true
            });
        }

        private void SetZoom(double zoom)
        {
            var matrix = _transform.Matrix;
            matrix.M11 = zoom;
            matrix.M22 = zoom;
            matrix.OffsetX = 0;
            matrix.OffsetY = 0;
            ConstrainPan(ref matrix);
            _transform.Matrix = matrix;
            UpdateZoomText();
        }

        private void ConstrainPan(ref Matrix matrix)
        {
            var zoom = Math.Max(1.0, matrix.M11);
            var maxX = Math.Max(0, ActualWidth * (zoom - 1));
            var maxY = Math.Max(0, ActualHeight * (zoom - 1));
            matrix.OffsetX = Math.Max(-maxX, Math.Min(maxX, matrix.OffsetX));
            matrix.OffsetY = Math.Max(-maxY, Math.Min(maxY, matrix.OffsetY));
        }

        private void ResetZoom() => SetZoom(1.0);

        private void UpdateZoomText()
        {
            ZoomText.Text = $"{Math.Round(CurrentZoom * 100):0}%";
        }

        private void UpdateFileInformation(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            FileNameText.Text = fileInfo.Name;
            InfoFileName.Text = fileInfo.Name;
            InfoDimensions.Text = PhotoImage.Source is BitmapSource bitmap
                ? $"{bitmap.PixelWidth} × {bitmap.PixelHeight} px"
                : "Dimensions unavailable";
            InfoDate.Text = $"Taken/created: {fileInfo.CreationTime:G}";
            InfoFileSize.Text = $"{fileInfo.Length / 1024d / 1024d:0.##} MB";
            var settings = new PhotoViewer.Services.SettingsService().LoadSettings();
            InfoLocation.Text = settings.ShowFileLocation
                ? fileInfo.DirectoryName ?? filePath
                : "Location hidden by settings";
        }

        private void NavigateImage(bool navigateNext)
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath)) return;

            var files = _navigationItems.Count > 0
                ? _navigationItems.Select(item => item.FilePath).ToList()
                : GetDirectoryFallbackFiles(_currentFilePath);

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

        private static List<string> GetDirectoryFallbackFiles(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (directory == null || !Directory.Exists(directory)) return new List<string>();
            return Directory.EnumerateFiles(directory)
                .Where(MediaFileTypes.IsSupported)
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Loads an image from the specified file path.
        /// </summary>
        public bool LoadImage(string filePath, bool resetEditSession = true)
        {
            try
            {
                _currentFilePath = filePath;
                if (resetEditSession)
                {
                    _editSession = new EditSession(filePath);
                    _editFlipTransform.ScaleX = 1;
                    MarkupCanvas.Strokes.Clear();
                    MarkupCanvas.Visibility = Visibility.Collapsed;
                    MarkupCanvas.IsHitTestVisible = false;
                    _suppressAdjustmentEvents = true;
                    ExposureSlider.Value = 0;
                    BrightnessSlider.Value = 0;
                    ContrastSlider.Value = 0;
                    HighlightsSlider.Value = 0;
                    ShadowsSlider.Value = 0;
                    SaturationSlider.Value = 0;
                    TemperatureSlider.Value = 0;
                    TintSlider.Value = 0;
                    SharpnessSlider.Value = 0;
                    _suppressAdjustmentEvents = false;
                }

                bool isVideo = MediaFileTypes.IsVideo(filePath);
                VideoPlayer.Stop();
                _videoProgressTimer.Stop();
                _videoIsPlaying = false;
                VideoControls.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
                VideoPlayButton.IsEnabled = true;
                VideoStatusText.Text = isVideo ? "Loading..." : string.Empty;
                PhotoImage.Visibility = isVideo ? Visibility.Collapsed : Visibility.Visible;
                VideoPlayer.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;

                if (isVideo)
                {
                    VideoPlayer.Source = new Uri(filePath);
                    VideoPlayer.Volume = VideoVolume.Value;
                    this.Title = System.IO.Path.GetFileName(filePath);
                    SelectCurrentFilmstripItem();
                    UpdateFileInformation(filePath);
                    UpdateEditAvailability();
                    UpdateEditStatus();
                    return true;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                // CacheOption.OnLoad ensures the file is not locked after loading.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                PhotoImage.Source = bitmap;
                _originalBitmap = bitmap;
                this.Title = System.IO.Path.GetFileName(filePath);
                SelectCurrentFilmstripItem();
                UpdateFileInformation(filePath);
                SetZoom(new PhotoViewer.Services.SettingsService().LoadSettings().DefaultViewerZoom);
                UpdateEditAvailability();
                UpdateEditStatus();
                UpdateZoomText();
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

        private void UpdateEditStatus()
        {
            if (EditStatusText == null) return;
            EditStatusText.Text = !CanEditCurrent
                ? "Read-only"
                : _editSession?.IsDirty == true
                ? $"Edited · rotation {_editSession.RotationDegrees}°{(_editSession.FlipHorizontal ? " · flipped" : string.Empty)}{(_editSession.CropAspectRatio.HasValue ? " · crop 4:3" : string.Empty)}"
                : "Original";
            UpdateEditAvailability();
        }

        private void UpdateEditAvailability()
        {
            var enabled = CanEditCurrent;
            SaveAsMenuItem.IsEnabled = enabled && !_isSavingCopy;
            AdjustmentsPanel.IsEnabled = enabled;
            ResetAdjustmentsButton.IsEnabled = enabled;
            UndoEditMenuItem.IsEnabled = enabled && (_editSession?.CanUndo == true);
            RedoEditMenuItem.IsEnabled = enabled && (_editSession?.CanRedo == true);
            ResetEditsMenuItem.IsEnabled = enabled;
            CancelEditsMenuItem.IsEnabled = enabled;
            FlipHorizontalMenuItem.IsEnabled = enabled;
            CropMenuItem.IsEnabled = enabled;
            CropFourThreeMenuItem.IsEnabled = enabled;
            CropSquareMenuItem.IsEnabled = enabled;
            CropSixteenNineMenuItem.IsEnabled = enabled;
            StraightenMenuItem.IsEnabled = enabled;
            FiltersMenuItem.IsEnabled = enabled;
            AnnotationMenuItem.IsEnabled = enabled;
            BackgroundMenuItem.IsEnabled = enabled;
            MarkupMenuItem.IsEnabled = enabled;
            ClearMarkupMenuItem.IsEnabled = enabled && (_editSession?.InkStrokeCount > 0);
            RotateEditMenuItem.IsEnabled = enabled;
            if (enabled)
            {
                EditUnavailableText.Visibility = Visibility.Collapsed;
            }
            else
            {
                EditUnavailableText.Text = IsCurrentVideo
                    ? "Video editing is not available in this viewer."
                    : "This provider is read-only; save a local copy before editing.";
                EditUnavailableText.Visibility = Visibility.Visible;
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
