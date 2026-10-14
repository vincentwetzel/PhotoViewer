namespace PhotoViewer.Services;

public readonly record struct CropRectangle(int X, int Y, int Width, int Height);

public static class ImageCropCalculator
{
    public static CropRectangle GetCenteredCrop(int width, int height, double aspectRatio)
    {
        if (width <= 0 || height <= 0 || aspectRatio <= 0 || double.IsNaN(aspectRatio) || double.IsInfinity(aspectRatio))
            return new CropRectangle(0, 0, Math.Max(0, width), Math.Max(0, height));

        var sourceAspect = (double)width / height;
        var cropWidth = width;
        var cropHeight = height;
        if (sourceAspect > aspectRatio)
            cropWidth = Math.Max(1, (int)Math.Round(height * aspectRatio));
        else if (sourceAspect < aspectRatio)
            cropHeight = Math.Max(1, (int)Math.Round(width / aspectRatio));

        return new CropRectangle(
            (width - cropWidth) / 2,
            (height - cropHeight) / 2,
            cropWidth,
            cropHeight);
    }
}
