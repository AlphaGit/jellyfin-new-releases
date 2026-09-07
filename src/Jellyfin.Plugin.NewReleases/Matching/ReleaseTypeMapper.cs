using Jellyfin.Plugin.NewReleases.Model;

namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>Type mapping, inclusion and display rules from <c>docs/domain_knowledge/release-types.md</c>.</summary>
public static class ReleaseTypeMapper
{
    public static (ReleaseType Primary, IReadOnlyList<ReleaseType> Secondaries) MapMusicBrainz(string? primary, IEnumerable<string> secondaries)
        => (MapMusicBrainzPrimary(primary), secondaries.Select(MapMusicBrainzSecondary).ToArray());

    /// <summary>Deezer <c>record_type</c>; Deezer expresses no secondary types.</summary>
    public static (ReleaseType Primary, IReadOnlyList<ReleaseType> Secondaries) MapDeezer(string? recordType)
        => (recordType switch
        {
            "album" => ReleaseType.Album,
            "ep" => ReleaseType.EP,
            "single" => ReleaseType.Single,
            "compile" => ReleaseType.Compilation,
            _ => ReleaseType.Other,
        }, Array.Empty<ReleaseType>());

    /// <summary>Included only when the primary and every secondary type are enabled; <see cref="ReleaseType.Other"/> anywhere excludes.</summary>
    public static bool IsIncluded(ReleaseType primary, IReadOnlyList<ReleaseType> secondaries, ISet<ReleaseType> enabled)
        => primary != ReleaseType.Other
            && enabled.Contains(primary)
            && secondaries.All(t => t != ReleaseType.Other && enabled.Contains(t));

    private static ReleaseType MapMusicBrainzPrimary(string? primary) => primary switch
    {
        "Album" => ReleaseType.Album,
        "EP" => ReleaseType.EP,
        "Single" => ReleaseType.Single,
        _ => ReleaseType.Other,
    };

    private static ReleaseType MapMusicBrainzSecondary(string secondary) => secondary switch
    {
        "Compilation" => ReleaseType.Compilation,
        "Live" => ReleaseType.Live,
        "Remix" => ReleaseType.Remix,
        "Soundtrack" => ReleaseType.Soundtrack,
        _ => ReleaseType.Other,
    };
}
