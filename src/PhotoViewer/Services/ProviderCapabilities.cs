namespace PhotoViewer.Services;

/// <summary>
/// Describes which operations a provider can honestly expose to the UI.
/// Read-only or unavailable providers are represented explicitly instead of
/// relying on file-path heuristics in individual commands.
/// </summary>
public interface IProviderCapabilities
{
    bool CanRead { get; }
    bool CanWrite { get; }
    bool CanDelete { get; }
    bool CanMove { get; }
    bool CanEdit { get; }
    bool IsOffline { get; }
}

public sealed record ProviderCapabilitySet(
    bool CanRead,
    bool CanWrite,
    bool CanDelete,
    bool CanMove,
    bool CanEdit,
    bool IsOffline) : IProviderCapabilities;

public static class ProviderCapabilities
{
    public static string Describe(IProviderCapabilities capabilities) =>
        !capabilities.CanRead ? "Unavailable" :
        capabilities.CanEdit && capabilities.CanWrite ? "Editable local source" :
        capabilities.IsOffline ? "Read-only/offline source" : "Read-only source";

    public static IProviderCapabilities For(IPhotoProvider provider) => provider switch
    {
        LocalFolderProvider => new ProviderCapabilitySet(true, true, true, true, true, false),
        FavoritesProvider or RecentlyViewedProvider => new ProviderCapabilitySet(true, false, false, false, false, false),
        _ => new ProviderCapabilitySet(true, false, false, false, false, true)
    };
}
