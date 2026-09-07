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

    [Fact]
    public void Decide_NoCandidate_IsMissingAndAsksForNoEditions()
    {
        var result = OwnershipMatcher.Decide(Release(), [], [Album("Homework", tracks: ["da funk"])]);

        Assert.Equal((OwnershipState.Missing, false, (Guid?)null, (long?)null), (result.State, result.NeedsEditions, result.LibraryAlbumId, result.EditionId));
    }

    [Fact]
    public void Decide_CandidateWithoutStoredEditions_AsksForEditionsInsteadOfDeciding()
    {
        var album = Album("Discovery", tracks: Discovery);

        var result = OwnershipMatcher.Decide(Release(), [], [album]);

        Assert.True(result.NeedsEditions);
        Assert.Equal((album.JellyfinId, "Title"), (result.LibraryAlbumId, result.MatchMethod));
    }

    [Fact]
    public void Decide_EveryEditionTrackMatchedByALibraryTrack_IsOwned()
    {
        var album = Album("Discovery", tracks: Discovery);
        var edition = Edition(7, "musicbrainz", "rel-1", Discovery);

        var result = OwnershipMatcher.Decide(Release(), [edition], [album]);

        Assert.Equal((OwnershipState.Owned, 7L, 0, false), (result.State, result.EditionId, result.MissingTracks.Count, result.NeedsEditions));
    }

    [Fact]
    public void Decide_EightOfTenTracksMatched_IsIncompleteNamingTheTwoMissingTitlesAndTheEdition()
    {
        var ten = Enumerable.Range(1, 10).Select(i => $"track {i}").ToArray();
        var album = Album("Ten Tracks", tracks: ten.Take(8).ToArray());
        var edition = Edition(42, "deezer", "dz-10", ten);

        var result = OwnershipMatcher.Decide(Release("ten tracks", "deezer", "dz-10"), [edition], [album]);

        Assert.Equal(OwnershipState.Incomplete, result.State);
        Assert.Equal(["track 9", "track 10"], result.MissingTracks);
        Assert.Equal(42L, result.EditionId);
    }

    [Fact]
    public void Decide_EditionChoice_MostMatchedThenFewestMissingThenMusicBrainzThenLowestId()
    {
        var album = Album("Discovery", tracks: ["a", "b", "c"]);
        Release release = Release();

        // more matched tracks wins (edition 2 matches 3, edition 1 matches 2)
        Assert.Equal(2L, OwnershipMatcher.Decide(release, [Edition(1, "musicbrainz", "x", "a", "b"), Edition(2, "deezer", "y", "a", "b", "c", "d")], [album]).EditionId);
        // equal matched → fewer missing wins
        Assert.Equal(4L, OwnershipMatcher.Decide(release, [Edition(3, "musicbrainz", "x", "a", "b", "c", "d", "e"), Edition(4, "deezer", "y", "a", "b", "c", "d")], [album]).EditionId);
        // still equal → MusicBrainz over Deezer
        Assert.Equal(6L, OwnershipMatcher.Decide(release, [Edition(5, "deezer", "y", "a", "b", "c"), Edition(6, "musicbrainz", "x", "a", "b", "c")], [album]).EditionId);
        // still equal → lowest source edition id
        Assert.Equal(8L, OwnershipMatcher.Decide(release, [Edition(7, "musicbrainz", "rel-b", "a", "b", "c"), Edition(8, "musicbrainz", "rel-a", "a", "b", "c")], [album]).EditionId);
    }
}
