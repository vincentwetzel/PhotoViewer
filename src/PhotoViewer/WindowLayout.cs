namespace PhotoViewer.Models
{
    /// <summary>
    /// Represents the entire workspace layout, containing a collection of all open photo windows.
    /// This is the root object for JSON serialization.
    /// </summary>
    public class WindowLayout
    {
        public List<PhotoWindowState> PhotoWindows { get; set; } = new List<PhotoWindowState>();
    }
}