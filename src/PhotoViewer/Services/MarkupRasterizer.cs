using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace PhotoViewer.Services;

public static class MarkupRasterizer
{
    public static BitmapSource Apply(BitmapSource source, IReadOnlyList<MarkupStroke> strokes)
    {
        if (strokes.Count == 0) return source;
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
            foreach (var stroke in strokes.Where(stroke => stroke.Points.Count > 1))
            {
                var pen = new Pen(new SolidColorBrush(Color.FromRgb(stroke.Red, stroke.Green, stroke.Blue)), stroke.Width)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                pen.Brush.Freeze();
                pen.Freeze();
                for (var index = 1; index < stroke.Points.Count; index++)
                {
                    var from = ToPixelPoint(stroke.Points[index - 1], source);
                    var to = ToPixelPoint(stroke.Points[index], source);
                    drawing.DrawLine(pen, from, to);
                }
            }
        }

        var rendered = new RenderTargetBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
        rendered.Render(visual);
        rendered.Freeze();
        return rendered;
    }

    private static Point ToPixelPoint(MarkupPoint point, BitmapSource source) =>
        new(Math.Clamp(point.X, 0, 1) * source.PixelWidth, Math.Clamp(point.Y, 0, 1) * source.PixelHeight);
}
