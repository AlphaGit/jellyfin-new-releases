using Jellyfin.Plugin.NewReleases.Model;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NewReleases.Configuration;

/// <summary>
/// Server-global plugin configuration, persisted as XML by Jellyfin's serializer. Scalar properties only (R9): a
/// missing element keeps the initializer's default, so older configuration files migrate forward without a seeding
/// step. Fields and defaults: <c>specs/001-track-new-releases/contracts/plugin-configuration.md</c>.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    public bool MusicBrainzEnabled { get; set; } = true;

    public bool DeezerEnabled { get; set; } = true;

    public bool IncludeAlbums { get; set; } = true;

    public bool IncludeEps { get; set; } = true;

    public bool IncludeSingles { get; set; }

    public bool IncludeCompilations { get; set; }

    public bool IncludeLive { get; set; }

    public bool IncludeRemixes { get; set; }

    public bool IncludeSoundtracks { get; set; }

    public string ReleasedSince { get; set; } = string.Empty;

    public string UserAgentContact { get; set; } = string.Empty;

    /// <summary>The admin's type selection as a set (FR-004).</summary>
    public ISet<ReleaseType> EnabledReleaseTypes()
    {
        var enabled = new HashSet<ReleaseType>();
        Add(IncludeAlbums, ReleaseType.Album);
        Add(IncludeEps, ReleaseType.EP);
        Add(IncludeSingles, ReleaseType.Single);
        Add(IncludeCompilations, ReleaseType.Compilation);
        Add(IncludeLive, ReleaseType.Live);
        Add(IncludeRemixes, ReleaseType.Remix);
        Add(IncludeSoundtracks, ReleaseType.Soundtrack);
        return enabled;

        void Add(bool include, ReleaseType type)
        {
            if (include)
            {
                enabled.Add(type);
            }
        }
    }
}
