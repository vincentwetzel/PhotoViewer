namespace PhotoViewer.Services;

public static class ImageAdjustmentProcessor
{
    public static void ApplyBgra32(byte[] pixels, ImageAdjustments adjustments)
    {
        if (adjustments.IsDefault) return;

        var exposureScale = Math.Pow(2, adjustments.Exposure);
        var contrastScale = 1 + adjustments.Contrast;
        var saturationScale = 1 + adjustments.Saturation;
        var original = adjustments.Sharpness > 0 ? (byte[])pixels.Clone() : pixels;
        var pixelCount = pixels.Length / 4;
        for (var index = 0; index + 3 < pixels.Length; index += 4)
        {
            var blue = pixels[index] / 255.0;
            var green = pixels[index + 1] / 255.0;
            var red = pixels[index + 2] / 255.0;

            red *= exposureScale;
            green *= exposureScale;
            blue *= exposureScale;
            red += adjustments.Brightness;
            green += adjustments.Brightness;
            blue += adjustments.Brightness;
            red = (red - 0.5) * contrastScale + 0.5;
            green = (green - 0.5) * contrastScale + 0.5;
            blue = (blue - 0.5) * contrastScale + 0.5;

            red += (1 - red) * Math.Max(0, adjustments.Highlights) - red * Math.Max(0, -adjustments.Highlights);
            green += (1 - green) * Math.Max(0, adjustments.Highlights) - green * Math.Max(0, -adjustments.Highlights);
            blue += (1 - blue) * Math.Max(0, adjustments.Highlights) - blue * Math.Max(0, -adjustments.Highlights);
            red += (1 - red) * Math.Max(0, adjustments.Shadows) - red * Math.Max(0, -adjustments.Shadows);
            green += (1 - green) * Math.Max(0, adjustments.Shadows) - green * Math.Max(0, -adjustments.Shadows);
            blue += (1 - blue) * Math.Max(0, adjustments.Shadows) - blue * Math.Max(0, -adjustments.Shadows);

            var luminance = red * 0.299 + green * 0.587 + blue * 0.114;
            red = luminance + (red - luminance) * saturationScale + adjustments.Tint * 0.1;
            green = luminance + (green - luminance) * saturationScale;
            blue = luminance + (blue - luminance) * saturationScale - adjustments.Tint * 0.1;
            red += adjustments.Temperature * 0.1;
            blue -= adjustments.Temperature * 0.1;

            if (adjustments.Sharpness > 0 && pixelCount > 1)
            {
                var neighboringIndex = index == 0 ? 4 : index - 4;
                var neighborLuminance = (original[neighboringIndex] * 0.114 + original[neighboringIndex + 1] * 0.587 + original[neighboringIndex + 2] * 0.299) / 255;
                var currentLuminance = red * 0.299 + green * 0.587 + blue * 0.114;
                var amount = adjustments.Sharpness * 0.5;
                red += (currentLuminance - neighborLuminance) * amount;
                green += (currentLuminance - neighborLuminance) * amount;
                blue += (currentLuminance - neighborLuminance) * amount;
            }

            pixels[index] = ToByte(blue);
            pixels[index + 1] = ToByte(green);
            pixels[index + 2] = ToByte(red);
        }
    }

    private static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
}
