namespace PhotoViewer.Services;

public enum ImageFilter { None, Grayscale, Sepia, HighContrast }

public static class ImageFilterProcessor
{
    public static void ApplyBgra32(byte[] pixels, ImageFilter filter)
    {
        if (filter == ImageFilter.None) return;
        for (var i = 0; i + 3 < pixels.Length; i += 4)
        {
            var b = pixels[i] / 255.0;
            var g = pixels[i + 1] / 255.0;
            var r = pixels[i + 2] / 255.0;
            var l = r * .299 + g * .587 + b * .114;
            if (filter == ImageFilter.Grayscale) r = g = b = l;
            else if (filter == ImageFilter.Sepia) { r = l * 1.07; g = l * .94; b = l * .75; }
            else { r = g = b = l < .5 ? l * .5 : .5 + (l - .5) * 1.8; }
            pixels[i] = Clamp(b); pixels[i + 1] = Clamp(g); pixels[i + 2] = Clamp(r);
        }
    }

    private static byte Clamp(double value) => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
}
