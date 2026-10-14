using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoViewer.Services;

public readonly record struct ImageAdjustments(double Exposure, double Brightness, double Contrast, double Highlights,
    double Shadows, double Saturation, double Temperature, double Tint, double Sharpness)
{
    public static ImageAdjustments Default => new();
    public bool IsDefault => this == Default;
}

public readonly record struct MarkupPoint(double X, double Y);
public sealed record MarkupStroke(IReadOnlyList<MarkupPoint> Points, byte Red, byte Green, byte Blue, double Width);

public readonly record struct ImageEditState(int RotationDegrees, bool FlipHorizontal, double? CropAspectRatio,
    double StraightenDegrees, ImageAdjustments Adjustments, ImageFilter Filter, string Annotation,
    IReadOnlyList<MarkupStroke> MarkupStrokes, ImageBackgroundFill BackgroundFill);
    

public sealed class EditSession
{
    private readonly Stack<ImageEditState> _undo = new();
    private readonly Stack<ImageEditState> _redo = new();
    public string OriginalPath { get; }
    public int RotationDegrees { get; private set; }
    public bool FlipHorizontal { get; private set; }
    public double? CropAspectRatio { get; private set; }
    public double StraightenDegrees { get; private set; }
    public ImageAdjustments Adjustments { get; private set; }
    public ImageFilter Filter { get; private set; }
    public string Annotation { get; private set; } = string.Empty;
    public IReadOnlyList<MarkupStroke> MarkupStrokes { get; private set; } = Array.Empty<MarkupStroke>();
    public int InkStrokeCount => MarkupStrokes.Count;
    public ImageBackgroundFill BackgroundFill { get; private set; }
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public bool IsDirty => RotationDegrees != 0 || FlipHorizontal || CropAspectRatio.HasValue ||
        Math.Abs(StraightenDegrees) > 0.0001 || !Adjustments.IsDefault || Filter != ImageFilter.None ||
        !string.IsNullOrEmpty(Annotation) || InkStrokeCount > 0 || BackgroundFill != ImageBackgroundFill.None;

    public EditSession(string originalPath) => OriginalPath = originalPath;

    public void RotateClockwise()
    {
        _undo.Push(CaptureState());
        _redo.Clear();
        RotationDegrees = (RotationDegrees + 90) % 360;
    }

    public void ToggleHorizontalFlip()
    {
        _undo.Push(CaptureState());
        _redo.Clear();
        FlipHorizontal = !FlipHorizontal;
    }

    public void SetCropAspectRatio(double? aspectRatio)
    {
        _undo.Push(CaptureState());
        _redo.Clear();
        CropAspectRatio = aspectRatio;
    }

    public void AdjustStraighten(double degrees)
    {
        _undo.Push(CaptureState());
        _redo.Clear();
        StraightenDegrees = Math.Clamp(StraightenDegrees + degrees, -45, 45);
    }

    public void SetAdjustments(ImageAdjustments adjustments)
    {
        if (Adjustments == adjustments) return;
        _undo.Push(CaptureState());
        _redo.Clear();
        Adjustments = adjustments;
    }

    public void SetFilter(ImageFilter filter)
    {
        if (Filter == filter) return;
        _undo.Push(CaptureState());
        _redo.Clear();
        Filter = filter;
    }

    public void SetAnnotation(string? annotation)
    {
        var normalized = annotation?.Trim() ?? string.Empty;
        if (Annotation == normalized) return;
        _undo.Push(CaptureState());
        _redo.Clear();
        Annotation = normalized;
    }

    public void AddInkStroke(MarkupStroke? stroke = null)
    {
        _undo.Push(CaptureState());
        _redo.Clear();
        MarkupStrokes = MarkupStrokes.Append(stroke ??
            new MarkupStroke(Array.Empty<MarkupPoint>(), 255, 255, 0, 3)).ToArray();
    }

    public void ClearInk()
    {
        if (InkStrokeCount == 0) return;
        _undo.Push(CaptureState());
        _redo.Clear();
        MarkupStrokes = Array.Empty<MarkupStroke>();
    }

    public void SetBackgroundFill(ImageBackgroundFill fill)
    {
        if (BackgroundFill == fill) return;
        _undo.Push(CaptureState());
        _redo.Clear();
        BackgroundFill = fill;
    }

    private ImageEditState CaptureState() =>
        new(RotationDegrees, FlipHorizontal, CropAspectRatio, StraightenDegrees, Adjustments, Filter, Annotation, MarkupStrokes, BackgroundFill);

    public bool Undo()
    {
        if (_undo.Count == 0) return false;
        _redo.Push(CaptureState());
        var state = _undo.Pop();
        RotationDegrees = state.RotationDegrees;
        FlipHorizontal = state.FlipHorizontal;
        CropAspectRatio = state.CropAspectRatio;
        StraightenDegrees = state.StraightenDegrees;
        Adjustments = state.Adjustments;
        Filter = state.Filter;
        Annotation = state.Annotation;
        MarkupStrokes = state.MarkupStrokes;
        BackgroundFill = state.BackgroundFill;
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0) return false;
        _undo.Push(CaptureState());
        var state = _redo.Pop();
        RotationDegrees = state.RotationDegrees;
        FlipHorizontal = state.FlipHorizontal;
        CropAspectRatio = state.CropAspectRatio;
        StraightenDegrees = state.StraightenDegrees;
        Adjustments = state.Adjustments;
        Filter = state.Filter;
        Annotation = state.Annotation;
        MarkupStrokes = state.MarkupStrokes;
        BackgroundFill = state.BackgroundFill;
        return true;
    }

    public void Reset()
    {
        _undo.Clear();
        _redo.Clear();
        RotationDegrees = 0;
        FlipHorizontal = false;
        CropAspectRatio = null;
        StraightenDegrees = 0;
        Adjustments = ImageAdjustments.Default;
        Filter = ImageFilter.None;
        Annotation = string.Empty;
        MarkupStrokes = Array.Empty<MarkupStroke>();
        BackgroundFill = ImageBackgroundFill.None;
    }
}

public interface IImageEditingService
{
    Task SaveEditedCopyAsync(string sourcePath, string destinationPath, int rotationDegrees, bool flipHorizontal, double? cropAspectRatio, double straightenDegrees,
        ImageAdjustments adjustments = default, ImageFilter filter = ImageFilter.None, IReadOnlyList<MarkupStroke>? markupStrokes = null, ImageBackgroundFill backgroundFill = ImageBackgroundFill.None,
        CancellationToken cancellationToken = default);
}

public sealed class ImageEditingService : IImageEditingService
{
    public async Task SaveEditedCopyAsync(string sourcePath, string destinationPath, int rotationDegrees, bool flipHorizontal, double? cropAspectRatio, double straightenDegrees,
        ImageAdjustments adjustments = default, ImageFilter filter = ImageFilter.None, IReadOnlyList<MarkupStroke>? markupStrokes = null, ImageBackgroundFill backgroundFill = ImageBackgroundFill.None,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(destinationPath))
            throw new IOException($"The destination file already exists: {destinationPath}");
        if (!IsSupportedDestination(destinationPath))
            throw new NotSupportedException("Choose a PNG, JPEG, BMP, or GIF destination file.");

        if (rotationDegrees % 360 == 0 && !flipHorizontal && !cropAspectRatio.HasValue && Math.Abs(straightenDegrees) < 0.0001 && adjustments.IsDefault && filter == ImageFilter.None && (markupStrokes == null || markupStrokes.Count == 0) && backgroundFill == ImageBackgroundFill.None &&
            CanCopyWithoutEncoding(sourcePath, destinationPath))
        {
            await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite: false), cancellationToken);
            return;
        }

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(sourcePath);
            bitmap.EndInit();
            bitmap.Freeze();

            BitmapSource working = bitmap;
            if (markupStrokes is { Count: > 0 })
                working = MarkupRasterizer.Apply(working, markupStrokes);
            if (!adjustments.IsDefault)
            {
                var converted = new FormatConvertedBitmap(working, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                converted.Freeze();
                var pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4];
                converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
                ImageAdjustmentProcessor.ApplyBgra32(pixels, adjustments);
                var adjusted = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, converted.DpiX, converted.DpiY,
                    System.Windows.Media.PixelFormats.Bgra32, null, pixels, converted.PixelWidth * 4);
                adjusted.Freeze();
                working = adjusted;
            }
            if (filter != ImageFilter.None)
            {
                var converted = new FormatConvertedBitmap(working, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                converted.Freeze();
                var pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4];
                converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
                ImageFilterProcessor.ApplyBgra32(pixels, filter);
                var filtered = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, converted.DpiX, converted.DpiY,
                    System.Windows.Media.PixelFormats.Bgra32, null, pixels, converted.PixelWidth * 4);
                filtered.Freeze();
                working = filtered;
            }
            if (backgroundFill != ImageBackgroundFill.None)
            {
                var converted = new FormatConvertedBitmap(working, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                converted.Freeze();
                var pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4];
                converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
                ImageBackgroundProcessor.ApplyBgra32(pixels, backgroundFill, converted.PixelWidth, converted.PixelHeight);
                var filled = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, converted.DpiX, converted.DpiY,
                    System.Windows.Media.PixelFormats.Bgra32, null, pixels, converted.PixelWidth * 4);
                filled.Freeze();
                working = filled;
            }
            if (cropAspectRatio is double aspect && aspect > 0 &&
                working.PixelWidth > 0 && working.PixelHeight > 0)
            {
                var crop = ImageCropCalculator.GetCenteredCrop(working.PixelWidth, working.PixelHeight, aspect);
                working = new CroppedBitmap(working, new System.Windows.Int32Rect(
                    crop.X, crop.Y, crop.Width, crop.Height));
                working.Freeze();
            }

            var transformGroup = new TransformGroup();
            if (flipHorizontal)
                transformGroup.Children.Add(new ScaleTransform(-1, 1, working.PixelWidth / 2.0, working.PixelHeight / 2.0));
            if (rotationDegrees % 360 != 0 || Math.Abs(straightenDegrees) > 0.0001)
                transformGroup.Children.Add(new RotateTransform(rotationDegrees + straightenDegrees));
            var transformed = new TransformedBitmap(working, transformGroup);
            transformed.Freeze();
            BitmapEncoder encoder = CreateEncoder(destinationPath);
            encoder.Frames.Add(BitmapFrame.Create(transformed));
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var stream = File.Create(destinationPath);
                encoder.Save(stream);
            }
            catch
            {
                try
                {
                    if (File.Exists(destinationPath)) File.Delete(destinationPath);
                }
                catch { }
                throw;
            }
        }, cancellationToken);
    }

    private static bool CanCopyWithoutEncoding(string sourcePath, string destinationPath)
    {
        var sourceExtension = Path.GetExtension(sourcePath);
        var destinationExtension = Path.GetExtension(destinationPath);
        if (sourceExtension.Equals(destinationExtension, StringComparison.OrdinalIgnoreCase)) return true;
        return IsJpeg(sourceExtension) && IsJpeg(destinationExtension);
    }

    private static bool IsJpeg(string extension) =>
        extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);

    private static BitmapEncoder CreateEncoder(string destinationPath) =>
        Path.GetExtension(destinationPath).ToLowerInvariant() switch
        {
            ".png" => new PngBitmapEncoder(),
            ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            ".gif" => new GifBitmapEncoder(),
            _ => throw new NotSupportedException("Choose a PNG, JPEG, BMP, or GIF destination file.")
        };

    private static bool IsSupportedDestination(string destinationPath) =>
        new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }
            .Contains(Path.GetExtension(destinationPath), StringComparer.OrdinalIgnoreCase);
}
