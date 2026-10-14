using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhotoViewer.Models;
using PhotoViewer.Services;
using PhotoViewer.ViewModels;

namespace PhotoViewer.Tests;

[TestClass]
public sealed class CoreBehaviorTests
{
    [DataTestMethod]
    [DataRow("photo.jpg", false)]
    [DataRow("clip.MP4", true)]
    [DataRow("animation.webm", true)]
    [DataRow("document.pdf", false)]
    public void PhotoItem_ClassifiesMediaByExtension(string fileName, bool expectedVideo)
    {
        var item = new PhotoItem(fileName, fileName, DateTime.UtcNow, 10);

        Assert.AreEqual(expectedVideo, item.IsVideo);
    }

    [TestMethod]
    public void MediaFileTypes_ClassifiesSupportedImagesAndVideos()
    {
        Assert.IsTrue(MediaFileTypes.IsSupported("photo.GIF"));
        Assert.IsTrue(MediaFileTypes.IsSupported("clip.WEBM"));
        Assert.IsTrue(MediaFileTypes.IsSupported(".jpg"));
        Assert.IsTrue(MediaFileTypes.IsSupported("C:\\Photos\\holiday.JpEg"));
        Assert.IsTrue(MediaFileTypes.IsVideo("C:\\Photos\\clip.MoV"));
        Assert.IsFalse(MediaFileTypes.IsSupported("notes.txt"));
        Assert.IsFalse(MediaFileTypes.IsSupported("C:\\Photos\\README"));
        Assert.IsFalse(MediaFileTypes.IsSupported(null));
    }

    [TestMethod]
    public void PhotoItem_UsesPixelDimensionsForAspectRatio()
    {
        var item = new PhotoItem("image.jpg", "image.jpg", DateTime.UtcNow, 10, 400, 200);

        Assert.AreEqual(2.0, item.AspectRatio);
    }

    [TestMethod]
    public void ImageCropCalculator_CentersCropForWideAndTallSources()
    {
        var wide = ImageCropCalculator.GetCenteredCrop(400, 200, 1);
        Assert.AreEqual(new CropRectangle(100, 0, 200, 200), wide);

        var tall = ImageCropCalculator.GetCenteredCrop(200, 400, 1);
        Assert.AreEqual(new CropRectangle(0, 100, 200, 200), tall);
    }

    [TestMethod]
    public void ImageCropCalculator_LeavesInvalidAspectAndMatchingAspectUntouched()
    {
        Assert.AreEqual(new CropRectangle(0, 0, 400, 200), ImageCropCalculator.GetCenteredCrop(400, 200, 0));
        Assert.AreEqual(new CropRectangle(0, 0, 400, 200), ImageCropCalculator.GetCenteredCrop(400, 200, double.NaN));
        Assert.AreEqual(new CropRectangle(0, 0, 400, 200), ImageCropCalculator.GetCenteredCrop(400, 200, 2));
    }

    [TestMethod]
    public async Task ImageEditingService_EncodesCroppedOutputWithExpectedDimensions()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerEditOutput");
        var sourcePath = Path.Combine(directory.FullName, "source.png");
        var outputPath = Path.Combine(directory.FullName, "edited.png");
        var source = new WriteableBitmap(4, 2, 96, 96, PixelFormats.Bgra32, null);
        source.WritePixels(new Int32Rect(0, 0, 4, 2), new byte[4 * 2 * 4], 4 * 4, 0);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using (var stream = File.Create(sourcePath))
            encoder.Save(stream);

        await new ImageEditingService().SaveEditedCopyAsync(sourcePath, outputPath, 0, false, 1, 0);

        var result = new BitmapImage(new Uri(outputPath));
        Assert.AreEqual(2, result.PixelWidth);
        Assert.AreEqual(2, result.PixelHeight);
    }

    [TestMethod]
    public async Task ImageEditingService_EncodesJpegOutput()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerJpegOutput");
        var sourcePath = Path.Combine(directory.FullName, "source.png");
        var outputPath = Path.Combine(directory.FullName, "edited.jpg");
        var source = new WriteableBitmap(2, 2, 96, 96, PixelFormats.Bgra32, null);
        source.WritePixels(new Int32Rect(0, 0, 2, 2), new byte[16], 8, 0);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using (var stream = File.Create(sourcePath))
            encoder.Save(stream);

        await new ImageEditingService().SaveEditedCopyAsync(sourcePath, outputPath, 0, false, null, 0);

        var result = new BitmapImage(new Uri(outputPath));
        Assert.AreEqual(2, result.PixelWidth);
        Assert.AreEqual(2, result.PixelHeight);
        Assert.AreEqual(".jpg", Path.GetExtension(outputPath));
    }

    [TestMethod]
    public async Task ImageEditingService_PreservesPngMetadataDuringProcessing()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerMetadataOutput");
        var sourcePath = Path.Combine(directory.FullName, "source.png");
        var outputPath = Path.Combine(directory.FullName, "edited.png");
        var source = new WriteableBitmap(2, 2, 96, 96, PixelFormats.Bgra32, null);
        source.WritePixels(new Int32Rect(0, 0, 2, 2), new byte[16], 8, 0);
        var metadata = new BitmapMetadata("png") { ApplicationName = "PhotoViewer test" };
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source, null, metadata, null));
        using (var stream = File.Create(sourcePath))
            encoder.Save(stream);

        await new ImageEditingService().SaveEditedCopyAsync(sourcePath, outputPath, 90, false, null, 0);

        var result = new BitmapImage(new Uri(outputPath));
        var resultMetadata = result.Metadata as BitmapMetadata;
        Assert.AreEqual("PhotoViewer test", resultMetadata?.ApplicationName);
    }

    [TestMethod]
    public async Task ImageEditingService_RejectsExistingDestination()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerExistingOutput");
        var sourcePath = Path.Combine(directory.FullName, "source.png");
        var outputPath = Path.Combine(directory.FullName, "existing.png");
        File.WriteAllText(sourcePath, "source");
        File.WriteAllText(outputPath, "keep me");

        await Assert.ThrowsExceptionAsync<IOException>(() =>
            new ImageEditingService().SaveEditedCopyAsync(sourcePath, outputPath, 90, false, null, 0));

        Assert.AreEqual("keep me", File.ReadAllText(outputPath));
    }

    [TestMethod]
    public async Task ImageEditingService_RejectsUnsupportedDestinationBeforeDecode()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerUnsupportedOutput");
        var sourcePath = Path.Combine(directory.FullName, "source.not-an-image");
        var outputPath = Path.Combine(directory.FullName, "edited.tiff");
        File.WriteAllText(sourcePath, "not an image");

        await Assert.ThrowsExceptionAsync<NotSupportedException>(() =>
            new ImageEditingService().SaveEditedCopyAsync(sourcePath, outputPath, 0, false, null, 0));
    }

    [TestMethod]
    public async Task ImageEditingService_HonorsCancellationBeforeExportWrite()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerCanceledOutput");
        var sourcePath = Path.Combine(directory.FullName, "source.png");
        var outputPath = Path.Combine(directory.FullName, "edited.png");
        File.WriteAllText(sourcePath, "not an image");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
            new ImageEditingService().SaveEditedCopyAsync(sourcePath, outputPath, 0, false, null, 0,
                cancellationToken: cancellation.Token));
        Assert.IsFalse(File.Exists(outputPath));
    }

    [TestMethod]
    public void LocalProvider_ExposesWritableCapabilities()
    {
        var capabilities = ProviderCapabilities.For(new LocalFolderProvider("C:\\Photos"));

        Assert.IsTrue(capabilities.CanRead);
        Assert.IsTrue(capabilities.CanWrite);
        Assert.IsTrue(capabilities.CanDelete);
        Assert.IsTrue(capabilities.CanMove);
        Assert.IsTrue(capabilities.CanEdit);
        Assert.IsFalse(capabilities.IsOffline);
        Assert.AreEqual("Editable local source", ProviderCapabilities.Describe(capabilities));
    }

    [TestMethod]
    public void CollectionProviders_AreReadableButReadOnly()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerCollectionCapabilities");
        var favorites = ProviderCapabilities.For(new FavoritesProvider(new FavoritesService(directory.FullName)));
        var recent = ProviderCapabilities.For(new RecentlyViewedProvider(new HistoryService(directory.FullName)));

        Assert.IsTrue(favorites.CanRead);
        Assert.IsFalse(favorites.CanWrite || favorites.CanDelete || favorites.CanMove || favorites.CanEdit);
        Assert.IsTrue(recent.CanRead);
        Assert.IsFalse(recent.CanWrite || recent.CanDelete || recent.CanMove || recent.CanEdit);
        Assert.AreEqual("Read-only source", ProviderCapabilities.Describe(favorites));
        Assert.AreEqual("Read-only source", ProviderCapabilities.Describe(recent));
    }

    [TestMethod]
    public void ProviderStatusFormatter_DistinguishesUnavailableAndReadyStates()
    {
        var provider = new FavoritesProvider(new FavoritesService());
        Assert.AreEqual("Unavailable", ProviderStatusFormatter.GetStatus(provider, "Unavailable"));
        Assert.AreEqual("Ready", ProviderStatusFormatter.GetStatus(provider, "Ready"));
    }

    [TestMethod]
    public void SourceItemViewModel_ClassifiesUnavailableFailuresAndSuccess()
    {
        var source = new SourceItemViewModel(new FavoritesProvider(new FavoritesService()));
        source.MarkLoadFailed(new InvalidOperationException("local source unavailable"));

        Assert.IsTrue(source.IsUnavailable);
        Assert.AreEqual("Unavailable", source.StatusText);

        source.MarkLoadSucceeded();
        Assert.IsFalse(source.IsUnavailable);
        Assert.IsNotNull(source.LastLoadedAt);
    }

    [TestMethod]
    public void EditSession_SupportsUndoRedoWithoutChangingOriginalPath()
    {
        var session = new EditSession("original.jpg");
        session.RotateClockwise();
        session.RotateClockwise();
        session.ToggleHorizontalFlip();
        session.SetCropAspectRatio(4.0 / 3.0);

        Assert.AreEqual("original.jpg", session.OriginalPath);
        Assert.IsTrue(session.CanUndo);
        Assert.IsFalse(session.CanRedo);
        Assert.AreEqual(180, session.RotationDegrees);
        Assert.IsTrue(session.FlipHorizontal);
        Assert.AreEqual(4.0 / 3.0, session.CropAspectRatio);
        Assert.IsTrue(session.Undo());
        Assert.IsTrue(session.CanRedo);
        Assert.AreEqual(180, session.RotationDegrees);
        Assert.IsTrue(session.FlipHorizontal);
        Assert.IsNull(session.CropAspectRatio);
        Assert.IsTrue(session.Redo());
        Assert.IsTrue(session.CanUndo);
        Assert.AreEqual(180, session.RotationDegrees);
        Assert.IsTrue(session.FlipHorizontal);
        Assert.AreEqual(4.0 / 3.0, session.CropAspectRatio);
    }

    [TestMethod]
    public void EditSession_ResetClearsDirtyStateAndHistory()
    {
        var session = new EditSession("original.jpg");
        session.RotateClockwise();
        session.Reset();

        Assert.IsFalse(session.IsDirty);
        Assert.AreEqual(0, session.RotationDegrees);
        Assert.IsFalse(session.Undo());
        Assert.IsFalse(session.Redo());
    }

    [TestMethod]
    public void EditSession_SupportsMultipleCropPresets()
    {
        var session = new EditSession("original.jpg");

        session.SetCropAspectRatio(1);
        Assert.AreEqual(1, session.CropAspectRatio);
        session.SetCropAspectRatio(16.0 / 9.0);
        Assert.AreEqual(16.0 / 9.0, session.CropAspectRatio);
        Assert.IsTrue(session.Undo());
        Assert.AreEqual(1, session.CropAspectRatio);
    }

    [TestMethod]
    public void EditSession_StraightenSupportsUndoRedoAndClampsAngle()
    {
        var session = new EditSession("original.jpg");

        session.AdjustStraighten(50);
        Assert.AreEqual(45, session.StraightenDegrees);
        Assert.IsTrue(session.Undo());
        Assert.AreEqual(0, session.StraightenDegrees);
        Assert.IsTrue(session.Redo());
        Assert.AreEqual(45, session.StraightenDegrees);
    }

    [TestMethod]
    public void EditSession_PreservesAdjustmentStateThroughUndoRedo()
    {
        var session = new EditSession("original.jpg");
        var adjustments = new ImageAdjustments(1, 0.2, 0.3, 0, 0, 0.4, 0, 0, 0.1);
        session.SetAdjustments(adjustments);

        Assert.AreEqual(adjustments, session.Adjustments);
        Assert.IsTrue(session.Undo());
        Assert.AreEqual(ImageAdjustments.Default, session.Adjustments);
        Assert.IsTrue(session.Redo());
        Assert.AreEqual(adjustments, session.Adjustments);
    }

    [TestMethod]
    public void ImageAdjustments_DefaultAndPresetHaveExpectedDirtyState()
    {
        Assert.IsTrue(ImageAdjustments.Default.IsDefault);
        var preset = new ImageAdjustments(0.1, 0.05, 0.1, -0.05, 0.1, 0.05, 0, 0, 0.05);
        Assert.IsFalse(preset.IsDefault);
    }

    [TestMethod]
    public void ImageAdjustmentProcessor_ChangesPixelsAndClampsChannels()
    {
        var pixels = new byte[] { 10, 20, 30, 255 };
        ImageAdjustmentProcessor.ApplyBgra32(pixels, new ImageAdjustments(0, 1, 0, 0, 0, 0, 0, 0, 0));

        Assert.AreEqual(255, pixels[0]);
        Assert.AreEqual(255, pixels[1]);
        Assert.AreEqual(255, pixels[2]);
        Assert.AreEqual(255, pixels[3]);
    }

    [TestMethod]
    public void ImageAdjustmentProcessor_SharpeningChangesEdgeContrast()
    {
        var pixels = new byte[]
        {
            20, 20, 20, 255,
            200, 200, 200, 255
        };
        ImageAdjustmentProcessor.ApplyBgra32(pixels, new ImageAdjustments(0, 0, 0, 0, 0, 0, 0, 0, 1));

        Assert.AreNotEqual(200, pixels[4]);
    }

    [TestMethod]
    public void ImageFilterProcessor_GrayscaleRemovesColor()
    {
        var pixels = new byte[] { 0, 0, 255, 255 };
        ImageFilterProcessor.ApplyBgra32(pixels, ImageFilter.Grayscale);

        Assert.AreEqual(pixels[1], pixels[0]);
        Assert.AreEqual(pixels[2], pixels[0]);
    }

    [TestMethod]
    public void ImageFilterProcessor_SepiaWarmsAndHighContrastSeparatesTones()
    {
        var sepia = new byte[] { 100, 100, 100, 255 };
        ImageFilterProcessor.ApplyBgra32(sepia, ImageFilter.Sepia);
        Assert.IsTrue(sepia[2] > sepia[0]);

        var contrast = new byte[] { 120, 120, 120, 255, 220, 220, 220, 255 };
        ImageFilterProcessor.ApplyBgra32(contrast, ImageFilter.HighContrast);
        Assert.IsTrue(contrast[4] - contrast[0] > 100);
    }

    [TestMethod]
    public void ImageBackgroundProcessor_FillsTransparentPixels()
    {
        var pixels = new byte[] { 0, 0, 255, 0 };
        ImageBackgroundProcessor.ApplyBgra32(pixels, ImageBackgroundFill.White);

        Assert.AreEqual(255, pixels[0]);
        Assert.AreEqual(255, pixels[1]);
        Assert.AreEqual(255, pixels[2]);
        Assert.AreEqual(255, pixels[3]);
    }

    [TestMethod]
    public void ImageBackgroundProcessor_RemovesConnectedBorderBackground()
    {
        var pixels = new byte[3 * 3 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 240;
            pixels[i + 1] = 240;
            pixels[i + 2] = 240;
            pixels[i + 3] = 255;
        }
        var center = 4 * 4;
        pixels[center] = 20;
        pixels[center + 1] = 40;
        pixels[center + 2] = 220;

        ImageBackgroundProcessor.ApplyBgra32(pixels, ImageBackgroundFill.SubjectWhite, 3, 3);

        Assert.AreEqual(255, pixels[3]);
        Assert.AreEqual(255, pixels[center + 3]);
        Assert.AreEqual(220, pixels[center + 2]);
    }

    [TestMethod]
    public void EditSession_PreservesBackgroundFillThroughUndoRedo()
    {
        var session = new EditSession("original.png");
        session.SetBackgroundFill(ImageBackgroundFill.Black);

        Assert.AreEqual(ImageBackgroundFill.Black, session.BackgroundFill);
        Assert.IsTrue(session.Undo());
        Assert.AreEqual(ImageBackgroundFill.None, session.BackgroundFill);
        Assert.IsTrue(session.Redo());
        Assert.AreEqual(ImageBackgroundFill.Black, session.BackgroundFill);
    }

    [TestMethod]
    public void EditSession_PreservesAnnotationThroughUndoRedo()
    {
        var session = new EditSession("original.jpg");
        session.SetAnnotation("Keep this note");

        Assert.AreEqual("Keep this note", session.Annotation);
        Assert.IsTrue(session.Undo());
        Assert.AreEqual(string.Empty, session.Annotation);
        Assert.IsTrue(session.Redo());
        Assert.AreEqual("Keep this note", session.Annotation);
    }

    [TestMethod]
    public void EditSession_DoesNotRecordNoOpChanges()
    {
        var session = new EditSession("original.jpg");
        session.SetFilter(ImageFilter.None);
        session.SetAdjustments(ImageAdjustments.Default);
        session.SetAnnotation(" ");

        Assert.IsFalse(session.CanUndo);
    }

    [TestMethod]
    public void EditSession_TracksAndClearsInkStrokeHistory()
    {
        var session = new EditSession("original.jpg");
        session.AddInkStroke();
        session.AddInkStroke();

        Assert.AreEqual(2, session.InkStrokeCount);
        session.ClearInk();
        Assert.AreEqual(0, session.InkStrokeCount);
        Assert.IsTrue(session.Undo());
        Assert.AreEqual(2, session.InkStrokeCount);
    }

    [TestMethod]
    public void MarkupRasterizer_RendersNormalizedStrokeAtBitmapSize()
    {
        var source = new WriteableBitmap(10, 10, 96, 96, PixelFormats.Bgra32, null);
        source.WritePixels(new Int32Rect(0, 0, 10, 10), new byte[400], 40, 0);
        var stroke = new MarkupStroke(
            new[] { new MarkupPoint(0, 0.5), new MarkupPoint(1, 0.5) }, 255, 0, 0, 2);

        var rendered = MarkupRasterizer.Apply(source, new[] { stroke });

        Assert.AreEqual(10, rendered.PixelWidth);
        Assert.AreEqual(10, rendered.PixelHeight);
    }

    [TestMethod]
    public void DiagnosticsService_WritesOfflineLocalLog()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerDiagnostics");
        var service = new DiagnosticsService(directory.FullName);
        service.LogUnhandledException(new InvalidOperationException("test failure"), "test");

        Assert.IsTrue(File.Exists(service.LogPath));
        var log = File.ReadAllText(service.LogPath);
        StringAssert.Contains(log, "test failure");
        StringAssert.Contains(log, "test");
    }

    [TestMethod]
    public void SettingsService_MigratesLegacySettingsWithoutVersion()
    {
        var directory = Directory.CreateTempSubdirectory("PhotoViewerSettingsTests");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "settings.json"), "{\"Theme\":\"Dark\"}");
            var service = new SettingsService(directory.FullName);

            var settings = service.LoadSettings();

            Assert.AreEqual(2, settings.Version);
            Assert.AreEqual("Dark", settings.Theme);
            Assert.IsTrue(File.ReadAllText(Path.Combine(directory.FullName, "settings.json")).Contains("\"Version\": 2"));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void SettingsService_PersistsReduceMotionPreference()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerMotionSettings");
        var service = new SettingsService(directory.FullName);
        service.SaveSettings(new AppSettings { ReduceMotion = true });

        Assert.IsTrue(service.LoadSettings().ReduceMotion);
    }

    [TestMethod]
    public void PhotoQueryMatcher_ComposesMediaAndDateSearch()
    {
        var date = new DateTime(2024, 5, 6);
        var photo = new PhotoItemViewModel(new PhotoItem("C:\\Photos\\holiday.jpg", "holiday.jpg", date, 10));
        var video = new PhotoItemViewModel(new PhotoItem("C:\\Photos\\clip.mp4", "clip.mp4", date, 10));

        Assert.IsTrue(PhotoQueryMatcher.Matches(photo, "Photos", "2024-05-06"));
        Assert.IsFalse(PhotoQueryMatcher.Matches(photo, "Videos", "holiday"));
        Assert.IsTrue(PhotoQueryMatcher.Matches(video, "Videos", "clip"));
    }

    [TestMethod]
    public void PhotoSortDescriptor_MapsSupportedSortsAndDirections()
    {
        Assert.AreEqual("Photo.FileName", PhotoSortDescriptor.GetPropertyName("Name"));
        Assert.AreEqual("Photo.DateTaken", PhotoSortDescriptor.GetPropertyName("Date taken"));
        Assert.AreEqual("Photo.CreationDate", PhotoSortDescriptor.GetPropertyName("Date created"));
        Assert.AreEqual("Photo.DateModified", PhotoSortDescriptor.GetPropertyName("Date modified"));
        Assert.AreEqual("Photo.FileSizeInBytes", PhotoSortDescriptor.GetPropertyName("File size"));
        Assert.AreEqual(ListSortDirection.Ascending, PhotoSortDescriptor.GetDirection("Ascending"));
        Assert.AreEqual(ListSortDirection.Descending, PhotoSortDescriptor.GetDirection("descending"));
    }

    [TestMethod]
    public void PhotoSortDescriptor_UsesActiveDateForGrouping()
    {
        var taken = new DateTime(2024, 1, 1);
        var created = new DateTime(2024, 2, 2);
        var modified = new DateTime(2024, 3, 3);
        var photo = new PhotoItem("photo.jpg", "photo.jpg", created, 1, dateTaken: taken, dateModified: modified);

        Assert.AreEqual(taken, PhotoSortDescriptor.GetGroupingDate(photo, "Date taken"));
        Assert.AreEqual(created, PhotoSortDescriptor.GetGroupingDate(photo, "Date created"));
        Assert.AreEqual(modified, PhotoSortDescriptor.GetGroupingDate(photo, "Date modified"));
    }

    [TestMethod]
    public void PhotoDateGrouping_LabelsTodayYesterdayAndUnavailable()
    {
        var today = new DateTime(2025, 6, 15);

        Assert.AreEqual("Today", PhotoDateGrouping.GetLabel(today.AddHours(8), today));
        Assert.AreEqual("Yesterday", PhotoDateGrouping.GetLabel(today.AddDays(-1).AddHours(8), today));
        Assert.AreEqual("Date unavailable", PhotoDateGrouping.GetLabel(default, today));
        Assert.AreEqual("June 13", PhotoDateGrouping.GetLabel(today.AddDays(-2), today));
        Assert.AreEqual("June 15, 2024", PhotoDateGrouping.GetLabel(new DateTime(2024, 6, 15), today));
        Assert.AreEqual("unavailable", PhotoDateGrouping.GetKey(default));
    }

    [TestMethod]
    public async Task ImportCopyService_ReportsCancellationWithoutCopying()
    {
        using var sourceDirectory = Directory.CreateTempSubdirectory("PhotoViewerImportSource");
        using var destinationDirectory = Directory.CreateTempSubdirectory("PhotoViewerImportDestination");
        var source = Path.Combine(sourceDirectory.FullName, "photo.jpg");
        File.WriteAllText(source, "photo");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var summary = await new ImportCopyService().CopyAsync(
            new[] { source }, destinationDirectory.FullName, cancellationToken: cancellation.Token);

        Assert.IsTrue(summary.Canceled);
        Assert.AreEqual(0, summary.Imported);
        Assert.IsFalse(File.Exists(Path.Combine(destinationDirectory.FullName, "photo.jpg")));
    }

    [TestMethod]
    public async Task ImportCopyService_ReportsOneBasedCompletionCounts()
    {
        using var sourceDirectory = Directory.CreateTempSubdirectory("PhotoViewerImportProgressSource");
        using var destinationDirectory = Directory.CreateTempSubdirectory("PhotoViewerImportProgressDestination");
        var source = Path.Combine(sourceDirectory.FullName, "photo.jpg");
        File.WriteAllText(source, "photo");
        var progress = new RecordingProgress<ImportCopyProgress>();

        var summary = await new ImportCopyService().CopyAsync(
            new[] { source }, destinationDirectory.FullName,
            progress);

        Assert.AreEqual(1, summary.Imported);
        Assert.AreEqual(1, progress.Last.Index);
        Assert.AreEqual(1, progress.Last.Total);
    }

    [TestMethod]
    public async Task ImportCopyService_SkipsExistingTargetsAndCountsFailures()
    {
        using var sourceDirectory = Directory.CreateTempSubdirectory("PhotoViewerImportDuplicateSource");
        using var destinationDirectory = Directory.CreateTempSubdirectory("PhotoViewerImportDuplicateDestination");
        var source = Path.Combine(sourceDirectory.FullName, "photo.jpg");
        var missing = Path.Combine(sourceDirectory.FullName, "missing.jpg");
        var target = Path.Combine(destinationDirectory.FullName, "photo.jpg");
        File.WriteAllText(source, "source");
        File.WriteAllText(target, "existing");

        var summary = await new ImportCopyService().CopyAsync(
            new[] { source, missing }, destinationDirectory.FullName);

        Assert.AreEqual(0, summary.Imported);
        Assert.AreEqual(1, summary.Skipped);
        Assert.AreEqual(1, summary.Failed);
        CollectionAssert.AreEqual(new[] { "missing.jpg" }, summary.FailedFiles.ToArray());
        Assert.AreEqual("existing", File.ReadAllText(target));
    }

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public T Last { get; private set; } = default!;
        public void Report(T value) => Last = value;
    }

    [TestMethod]
    public void FavoritesService_DeduplicatesCaseInsensitivePathsAndPersists()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerFavorites");
        var service = new FavoritesService(directory.FullName);
        service.AddFavorite("C:\\Photos\\Holiday.jpg");
        service.AddFavorite("c:\\photos\\holiday.JPG");

        Assert.IsTrue(service.IsFavorite("C:\\PHOTOS\\HOLIDAY.JPG"));
        Assert.AreEqual(1, service.GetFavorites().Count());

        var reloaded = new FavoritesService(directory.FullName);
        Assert.AreEqual(1, reloaded.GetFavorites().Count());
        reloaded.RemoveFavorite("c:\\photos\\holiday.jpg");
        Assert.IsFalse(reloaded.IsFavorite("C:\\Photos\\Holiday.jpg"));
    }

    [TestMethod]
    public void HistoryService_MovesRepeatedPathsToMostRecentAndCapsSize()
    {
        using var directory = Directory.CreateTempSubdirectory("PhotoViewerHistory");
        var service = new HistoryService(directory.FullName);
        service.AddToHistory("first.jpg");
        service.AddToHistory("second.jpg");
        service.AddToHistory("FIRST.JPG");

        var history = service.GetHistory().ToList();
        Assert.AreEqual("FIRST.JPG", history[0]);
        Assert.AreEqual("second.jpg", history[1]);
        Assert.AreEqual(2, history.Count);
    }

    [TestMethod]
    public void PhotoSelectionService_SelectsClearsAndCountsItems()
    {
        var photos = new[]
        {
            new PhotoItemViewModel(new PhotoItem("one.jpg", "one.jpg", DateTime.UtcNow, 1)),
            new PhotoItemViewModel(new PhotoItem("two.jpg", "two.jpg", DateTime.UtcNow, 1)),
            new PhotoItemViewModel(new PhotoItem("three.jpg", "three.jpg", DateTime.UtcNow, 1))
        };
        photos[1].IsSelected = true;

        Assert.AreEqual(1, PhotoSelectionService.Count(photos));
        PhotoSelectionService.SelectAll(photos);
        Assert.AreEqual(3, PhotoSelectionService.Count(photos));
        PhotoSelectionService.Clear(photos);
        Assert.AreEqual(0, PhotoSelectionService.Count(photos));
    }
}
