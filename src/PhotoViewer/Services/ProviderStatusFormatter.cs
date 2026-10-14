namespace PhotoViewer.Services;

public static class ProviderStatusFormatter
{
    public static string GetStatus(IPhotoProvider provider, string loadState, DateTime? lastLoadedAt = null)
    {
        if (string.Equals(loadState, "Unavailable", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(loadState, "Offline", StringComparison.OrdinalIgnoreCase))
            return "Unavailable";
        if (provider is LocalFolderProvider) return "Local folder";
        return "Ready";
    }
}
