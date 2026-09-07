using Jellyfin.Plugin.NewReleases.Model;

namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>Type mapping, inclusion and display rules from <c>docs/domain_knowledge/release-types.md</c>.</summary>
public static class ReleaseTypeMapper
{
    public static (ReleaseType Primary, IReadOnlyList<ReleaseType> Secondaries) MapMusicBrainz(string? primary, IEnumerable<string> secondaries)
        => (MapMusicBrainzPrimary(primary), secondaries.Select(MapMusicBrainzSecondary).ToArray());

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
