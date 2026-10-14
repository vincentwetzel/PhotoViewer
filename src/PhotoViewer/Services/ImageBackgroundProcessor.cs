namespace PhotoViewer.Services;

public enum ImageBackgroundFill { None, White, Black, RemoveSubject, SubjectWhite, SubjectBlack }

public static class ImageBackgroundProcessor
{
    public static void ApplyBgra32(byte[] pixels, ImageBackgroundFill fill, int width = 0, int height = 0)
    {
        if (fill == ImageBackgroundFill.None) return;
        if (fill is ImageBackgroundFill.RemoveSubject or ImageBackgroundFill.SubjectWhite or ImageBackgroundFill.SubjectBlack)
            RemoveConnectedBorderBackground(pixels, width, height);

        if (fill == ImageBackgroundFill.RemoveSubject) return;
        var background = fill is ImageBackgroundFill.White or ImageBackgroundFill.SubjectWhite ? 255 : 0;
        for (var i = 0; i + 3 < pixels.Length; i += 4)
        {
            var alpha = pixels[i + 3] / 255.0;
            pixels[i] = Blend(pixels[i], background, alpha);
            pixels[i + 1] = Blend(pixels[i + 1], background, alpha);
            pixels[i + 2] = Blend(pixels[i + 2], background, alpha);
            pixels[i + 3] = 255;
        }
    }

    private static void RemoveConnectedBorderBackground(byte[] pixels, int width, int height)
    {
        var pixelCount = pixels.Length / 4;
        if (width <= 0 || height <= 0 || width * height != pixelCount || width < 2 || height < 2) return;

        var background = GetBorderAverage(pixels, width, height);
        const double tolerance = 52;
        var visited = new bool[pixelCount];
        var queue = new Queue<int>();

        for (var y = 0; y < height; y++)
        {
            AddIfBackground(y * width, pixels, background, tolerance, visited, queue);
            AddIfBackground(y * width + width - 1, pixels, background, tolerance, visited, queue);
        }
        for (var x = 0; x < width; x++)
        {
            AddIfBackground(x, pixels, background, tolerance, visited, queue);
            AddIfBackground((height - 1) * width + x, pixels, background, tolerance, visited, queue);
        }

        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            var offset = index * 4;
            pixels[offset + 3] = 0;
            var x = index % width;
            var y = index / width;
            if (x > 0) AddIfBackground(index - 1, pixels, background, tolerance, visited, queue);
            if (x + 1 < width) AddIfBackground(index + 1, pixels, background, tolerance, visited, queue);
            if (y > 0) AddIfBackground(index - width, pixels, background, tolerance, visited, queue);
            if (y + 1 < height) AddIfBackground(index + width, pixels, background, tolerance, visited, queue);
        }
    }

    private static void AddIfBackground(int index, byte[] pixels, (double Blue, double Green, double Red) background,
        double tolerance, bool[] visited, Queue<int> queue)
    {
        if (visited[index]) return;
        var offset = index * 4;
        visited[index] = true;
        if (pixels[offset + 3] == 0 || ColorDistance(pixels, offset, background) <= tolerance)
            queue.Enqueue(index);
    }

    private static (double Blue, double Green, double Red) GetBorderAverage(byte[] pixels, int width, int height)
    {
        double blue = 0, green = 0, red = 0;
        var count = 0;
        for (var y = 0; y < height; y++)
        {
            AddSample(y * width, pixels, ref blue, ref green, ref red, ref count);
            AddSample(y * width + width - 1, pixels, ref blue, ref green, ref red, ref count);
        }
        for (var x = 1; x + 1 < width; x++)
        {
            AddSample(x, pixels, ref blue, ref green, ref red, ref count);
            AddSample((height - 1) * width + x, pixels, ref blue, ref green, ref red, ref count);
        }
        return count == 0 ? (0, 0, 0) : (blue / count, green / count, red / count);
    }

    private static void AddSample(int index, byte[] pixels, ref double blue, ref double green, ref double red, ref int count)
    {
        var offset = index * 4;
        if (pixels[offset + 3] == 0) return;
        blue += pixels[offset];
        green += pixels[offset + 1];
        red += pixels[offset + 2];
        count++;
    }

    private static double ColorDistance(byte[] pixels, int offset, (double Blue, double Green, double Red) background)
    {
        var blue = pixels[offset] - background.Blue;
        var green = pixels[offset + 1] - background.Green;
        var red = pixels[offset + 2] - background.Red;
        return Math.Sqrt(blue * blue + green * green + red * red);
    }

    private static byte Blend(byte value, int background, double alpha) =>
        (byte)Math.Clamp((int)Math.Round(value * alpha + background * (1 - alpha)), 0, 255);
}
