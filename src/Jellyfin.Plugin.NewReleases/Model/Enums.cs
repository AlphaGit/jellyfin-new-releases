namespace Jellyfin.Plugin.NewReleases.Model;

/// <summary>Release types the plugin knows. <see cref="Other"/> marks a source type outside the list and always excludes.</summary>
public enum ReleaseType
{
    Album,
    EP,
    Single,
    Compilation,
    Live,
    Remix,
    Soundtrack,
    Other,
}
