using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

public sealed class ReleaseRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 3, 0, 0, TimeSpan.Zero);
    private const long Run1 = 1;

    private TestDatabase _db = null!;
    private long _artist;

    public async Task InitializeAsync()
    {
        _db = await TestDatabase.CreateAsync();
        _artist = await _db.Artists.UpsertAsync(ArtistRepositoryTests.Artist("name:daft punk", "Daft Punk"), CancellationToken.None);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    internal static CatalogueItem MusicBrainzItem(string title, string id = "rg-1", string? date = "2001-03-12", ReleaseType primary = ReleaseType.Album, params ReleaseType[] secondaries)
        => new(id, title, $"https://musicbrainz.org/release-group/{id}", primary, secondaries, date);

    internal static CatalogueItem DeezerItem(string title, string id = "dz-1", string? date = "2001-03-12", ReleaseType primary = ReleaseType.Album)
        => new(id, title, $"https://www.deezer.com/album/{id}", primary, [], date);

    [Fact]
    public async Task UpsertFromSourceAsync_SameNormalizedTitleFromTwoSources_OneReleaseTwoEntries()
    {
        var fromMusicBrainz = await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Discovery"), Run1, Now, CancellationToken.None);
        var fromDeezer = await _db.Releases.UpsertFromSourceAsync(_artist, "deezer", DeezerItem("DISCOVERY (Deluxe Edition)"), Run1, Now, CancellationToken.None);

        Assert.Equal(fromMusicBrainz, fromDeezer);
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT COUNT(*) FROM release"));
        Assert.Equal(2L, await _db.ScalarAsync<long>($"SELECT COUNT(*) FROM source_entry WHERE release_id = {fromMusicBrainz}"));
    }

    [Fact]
    public async Task UpsertFromSourceAsync_MusicBrainzEntryIsCanonicalForSourceIdTypesAndDate()
    {
        var id = await _db.Releases.UpsertFromSourceAsync(_artist, "deezer", DeezerItem("Alive 1997", "dz-9", "2001-10-01", ReleaseType.Album), Run1, Now, CancellationToken.None);
        await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Alive 1997", "rg-9", "2001-10-02", ReleaseType.Album, ReleaseType.Live), Run1, Now, CancellationToken.None);

        var release = (await _db.Releases.GetAsync(id, CancellationToken.None))!;

        Assert.Equal(("musicbrainz", "rg-9", ReleaseType.Album, "2001-10-02"), (release.CanonicalSource, release.CanonicalSourceId, release.PrimaryType, release.ReleaseDate));
        Assert.Equal([ReleaseType.Live], release.SecondaryTypes);
    }

    [Fact]
    public async Task PruneEntriesAsync_RemovingTheMusicBrainzEntryMakesDeezerCanonical()
    {
        var id = await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Homework", "rg-2", "1997-01-20"), Run1, Now, CancellationToken.None);
        await _db.Releases.UpsertFromSourceAsync(_artist, "deezer", DeezerItem("Homework", "dz-2", "1997-01-17"), Run1, Now, CancellationToken.None);

        // Run 2: Deezer still lists it, MusicBrainz (Complete fetch) no longer does.
        await _db.Releases.UpsertFromSourceAsync(_artist, "deezer", DeezerItem("Homework", "dz-2", "1997-01-17"), Run1 + 1, Now, CancellationToken.None);
        await _db.Releases.PruneEntriesAsync(_artist, "musicbrainz", Run1 + 1, CancellationToken.None);

        var release = (await _db.Releases.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("deezer", "dz-2", "1997-01-17"), (release.CanonicalSource, release.CanonicalSourceId, release.ReleaseDate));
    }

    [Fact]
    public async Task UpsertFromSourceAsync_MusicBrainzWithoutDateTakesDeezerDate()
    {
        var id = await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Human After All", "rg-3", date: null), Run1, Now, CancellationToken.None);
        await _db.Releases.UpsertFromSourceAsync(_artist, "deezer", DeezerItem("Human After All", "dz-3", "2005-03-14"), Run1, Now, CancellationToken.None);

        var release = (await _db.Releases.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("musicbrainz", "2005-03-14"), (release.CanonicalSource, release.ReleaseDate));
    }
}
