using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Matching;

/// <summary>Algorithm steps 1–5 of data-model.md "Ownership algorithm".</summary>
public class OwnershipMatcherTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 3, 0, 0, TimeSpan.Zero);
    private const string ReleaseGroup = "48117b90-a16e-34ca-a514-19c702df1158";
    private static readonly string[] Discovery = ["one more time", "aerodynamic", "digital love", "harder better faster stronger"];

    internal static Release Release(string normalizedTitle = "discovery", string canonicalSource = "musicbrainz", string canonicalId = ReleaseGroup)
        => new(1, 1, normalizedTitle, "Discovery", canonicalSource, canonicalId, ReleaseType.Album, [], "2001-02-26", "2001-02-26", Now, Now, OwnershipState.Missing, null, null, null, [], null);

    internal static Edition Edition(long id, string source, string editionId, params string[] tracks)
        => new(id, 1, source, editionId, "Discovery", tracks, Now);

    internal static LibraryAlbumSnapshot Album(string title, string? mbAlbum = null, string? mbReleaseGroup = null, params string[] tracks)
        => new(Guid.NewGuid(), title, TitleNormalizer.NormalizeAlbum(title), mbAlbum, mbReleaseGroup, tracks);

    [Fact]
    public void Decide_AlbumWithTheCanonicalReleaseGroupId_IsTheCandidateByIdentifier()
    {
        var album = Album("Some Other Title", mbReleaseGroup: ReleaseGroup, tracks: Discovery);
        var decoy = Album("Discovery", tracks: Discovery);

        var result = OwnershipMatcher.Decide(Release(), [Edition(1, "musicbrainz", "rel-1", Discovery)], [decoy, album]);

        Assert.Equal((album.JellyfinId, "Identifier"), (result.LibraryAlbumId, result.MatchMethod));
    }

    [Fact]
    public void Decide_AlbumWhoseReleaseIdIsAStoredMusicBrainzEdition_IsTheCandidateByIdentifier()
    {
        var album = Album("Retitled Discovery", mbAlbum: "rel-jp", tracks: Discovery);
        var editions = new[] { Edition(1, "musicbrainz", "rel-fr", Discovery), Edition(2, "musicbrainz", "rel-jp", Discovery) };

        var result = OwnershipMatcher.Decide(Release(), editions, [Album("Discovery", tracks: Discovery), album]);

        Assert.Equal((album.JellyfinId, "Identifier"), (result.LibraryAlbumId, result.MatchMethod));
    }

    [Fact]
    public void Decide_WithoutIdentifierMatch_AlbumTitledWithAnEditionQualifier_IsTheCandidateByTitle()
    {
        var album = Album("Discovery (Deluxe Edition)", tracks: Discovery);

        var result = OwnershipMatcher.Decide(Release(), [Edition(1, "musicbrainz", "rel-1", Discovery)], [Album("Homework", tracks: ["da funk"]), album]);

        Assert.Equal((album.JellyfinId, "Title"), (result.LibraryAlbumId, result.MatchMethod));
    }
}
