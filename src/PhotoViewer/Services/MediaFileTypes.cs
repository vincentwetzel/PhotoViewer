namespace PhotoViewer.Services;

public static class MediaFileTypes
{
    public static readonly IReadOnlySet<string> ImageExtensions =
        new HashSet<string>(new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> VideoExtensions =
        new HashSet<string>(new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" }, StringComparer.OrdinalIgnoreCase);

    public static bool IsVideo(string? path) => VideoExtensions.Contains(GetExtension(path));
    public static bool IsSupported(string? path) => ImageExtensions.Contains(GetExtension(path)) || IsVideo(path);

    private static string GetExtension(string? path) =>
        path?.StartsWith('.') == true && !path.Contains(Path.DirectorySeparatorChar) && !path.Contains(Path.AltDirectorySeparatorChar)
            ? path
            : Path.GetExtension(path);
}
