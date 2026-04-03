namespace PhotoViewer.Models
{
    /// <summary>
    /// Represents the state of a single photo window for serialization.
    /// This is a Plain Old C# Object (POCO) used for storing layout data.
    /// </summary>
    public class PhotoWindowState
    {
        public required string FilePath { get; set; }

        public double Top { get; set; }

        public double Left { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        // Default to 1.0 (100%) zoom if not specified.
        public double ZoomLevel { get; set; } = 1.0;

        // Default to no panning if not specified.
        public double PanOffsetX { get; set; } = 0.0;

        public double PanOffsetY { get; set; } = 0.0;
    }
}