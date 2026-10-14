using System.Text;

namespace PhotoViewer.Services;

public sealed class DiagnosticsService
{
    private readonly string _logPath;

    public DiagnosticsService(string? storageDirectory = null)
    {
        var root = storageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoViewer");
        Directory.CreateDirectory(root);
        _logPath = Path.Combine(root, "diagnostics.log");
    }

    public string LogPath => _logPath;

    public void LogUnhandledException(Exception exception, string source)
    {
        try
        {
            var entry = new StringBuilder()
                .AppendLine($"[{DateTime.UtcNow:O}] {source}")
                .AppendLine(exception.ToString())
                .AppendLine()
                .ToString();
            File.AppendAllText(_logPath, entry);
        }
        catch
        {
            // Diagnostics must never become a second application failure.
        }
    }
}
