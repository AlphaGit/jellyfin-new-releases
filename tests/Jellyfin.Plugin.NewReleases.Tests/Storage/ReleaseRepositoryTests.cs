using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

public sealed class ReleaseRepositoryTests : IAsyncLifetime
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public ReleaseRepositoryTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly DateTimeOffset Now = new(2026, 9, 6, 3, 0, 0, TimeSpan.Zero);
    private const long Run1 = 1;

    private static readonly DateOnly Today = new(2026, 9, 6);
    private static readonly ReleaseFilter DefaultFilter = new(new HashSet<ReleaseType> { ReleaseType.Album, ReleaseType.EP }, Today);

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

    [Fact]
    public async Task PruneEntriesAsync_DeletesOnlyThePairsStaleEntries()
    {
        var other = await _db.Artists.UpsertAsync(ArtistRepositoryTests.Artist("name:other", "Other"), CancellationToken.None);
        await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Stale", "rg-s"), Run1, Now, CancellationToken.None);
        await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Fresh", "rg-f"), Run1, Now, CancellationToken.None);
        await _db.Releases.UpsertFromSourceAsync(_artist, "deezer", DeezerItem("Stale", "dz-s"), Run1, Now, CancellationToken.None);
        await _db.Releases.UpsertFromSourceAsync(other, "musicbrainz", MusicBrainzItem("Elsewhere", "rg-e"), Run1, Now, CancellationToken.None);

        await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Fresh", "rg-f"), Run1 + 1, Now, CancellationToken.None);
        await _db.Releases.PruneEntriesAsync(_artist, "musicbrainz", Run1 + 1, CancellationToken.None);

        Assert.Equal(
            ["deezer:dz-s", "musicbrainz:rg-e", "musicbrainz:rg-f"],
            (await _db.ColumnAsync<string>("SELECT source || ':' || source_release_id FROM source_entry ORDER BY 1")));
    }

    [Fact]
    public async Task PruneEntriesAsync_DeletesReleasesLeftWithoutEntriesAndKeepsTheOthers()
    {
        var orphan = await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Orphan", "rg-o"), Run1, Now, CancellationToken.None);
        var shared = await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Shared", "rg-sh"), Run1, Now, CancellationToken.None);
        await _db.Releases.UpsertFromSourceAsync(_artist, "deezer", DeezerItem("Shared", "dz-sh"), Run1, Now, CancellationToken.None);

        await _db.Releases.PruneEntriesAsync(_artist, "musicbrainz", Run1 + 1, CancellationToken.None);

        Assert.Null(await _db.Releases.GetAsync(orphan, CancellationToken.None));
        Assert.NotNull(await _db.Releases.GetAsync(shared, CancellationToken.None));
    }

    [Theory]
    [InlineData("2024", "2024-00-00")]
    [InlineData("2024-05", "2024-05-00")]
    [InlineData("2024-05-17", "2024-05-17")]
    [InlineData(null, null)]
    public async Task UpsertFromSourceAsync_PadsDateSortAndLeavesUndatedNull(string? date, string? expectedSort)
    {
        var id = await _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem("Dated " + (date ?? "none"), "rg-" + (date ?? "none"), date), Run1, Now, CancellationToken.None);

        Assert.Equal(expectedSort, (await _db.Releases.GetAsync(id, CancellationToken.None))!.DateSort);
    }

    private Task<long> Seed(string title, string? date, string id, ReleaseType primary = ReleaseType.Album, params ReleaseType[] secondaries)
        => _db.Releases.UpsertFromSourceAsync(_artist, "musicbrainz", MusicBrainzItem(title, id, date, primary, secondaries), Run1, Now, CancellationToken.None);

    [Fact]
    public async Task ListAsync_OrdersByDateDescendingUndatedLastTitleTiebreak_YearOnlyAfterDated()
    {
        await Seed("Undated", null, "rg-u");
        await Seed("Beta 2023", "2023-06-01", "rg-b");
        await Seed("Year Only 2024", "2024", "rg-y");
        await Seed("Alpha 2023", "2023-06-01", "rg-a");
        await Seed("New Year 2024", "2024-01-01", "rg-n");

        var titles = (await _db.Releases.ListAsync(DefaultFilter, CancellationToken.None)).Select(r => r.Title);

        Assert.Equal(["New Year 2024", "Year Only 2024", "Alpha 2023", "Beta 2023", "Undated"], titles);
    }

    [Fact]
    public async Task ListAsync_NeverReturnsOwnedReleases()
    {
        var owned = await Seed("In Library", "2020-01-01", "rg-owned");
        await Seed("Missing One", "2020-01-02", "rg-missing");
        await _db.ExecuteAsync($"UPDATE release SET ownership_state = 'Owned' WHERE id = {owned}");

        var titles = (await _db.Releases.ListAsync(DefaultFilter, CancellationToken.None)).Select(r => r.Title);

        Assert.Equal(["Missing One"], titles);
    }

    [Fact]
    public async Task ListAsync_AppliesTheEnabledTypeSetAtReadTime()
    {
        await Seed("Studio", "2020-01-01", "rg-studio");
        await Seed("Live Set", "2020-01-02", "rg-live", ReleaseType.Album, ReleaseType.Live);

        var byDefault = await _db.Releases.ListAsync(DefaultFilter, CancellationToken.None);
        var withLive = await _db.Releases.ListAsync(DefaultFilter with { EnabledTypes = new HashSet<ReleaseType> { ReleaseType.Album, ReleaseType.EP, ReleaseType.Live } }, CancellationToken.None);

        Assert.Equal(["Studio"], byDefault.Select(r => r.Title));
        Assert.Equal([("Live Set", ReleaseType.Live), ("Studio", ReleaseType.Album)], withLive.Select(r => (r.Title, r.Type)));
    }

    [Fact]
    public async Task ListAsync_ReleasedSinceIsInclusiveAndKeepsUndatedRows()
    {
        await Seed("On The Day", "2020-01-01", "rg-on");
        await Seed("Day Before", "2019-12-31", "rg-before");
        await Seed("Undated", null, "rg-undated");

        var titles = (await _db.Releases.ListAsync(DefaultFilter with { ReleasedSince = new DateOnly(2020, 1, 1) }, CancellationToken.None)).Select(r => r.Title);

        Assert.Equal(["On The Day", "Undated"], titles);
    }

    [Fact]
    public async Task ListAsync_FiltersByArtistTypeStateAndInclusiveDateRange()
    {
        var otherSnapshot = ArtistRepositoryTests.Artist("name:other", "Other");
        var other = await _db.Artists.UpsertAsync(otherSnapshot, CancellationToken.None);
        var mine = (await _db.Artists.GetAllAsync(CancellationToken.None)).Single(a => a.Name == "Daft Punk").JellyfinId;
        var incomplete = await Seed("Incomplete Album", "2020-05-01", "rg-inc");
        await _db.ExecuteAsync($"UPDATE release SET ownership_state = 'Incomplete' WHERE id = {incomplete}");
        await Seed("Missing Album", "2020-06-01", "rg-mis");
        await Seed("An EP", "2020-07-01", "rg-ep", ReleaseType.EP);
        await Seed("Future Album", "2026-09-07", "rg-fut");
        await Seed("Today Album", "2026-09-06", "rg-today");
        await Seed("Undated", null, "rg-und");
        await _db.Releases.UpsertFromSourceAsync(other, "musicbrainz", MusicBrainzItem("Other Artist Album", "rg-other", "2020-06-15"), Run1, Now, CancellationToken.None);

        async Task<string[]> Titles(ReleaseFilter f) => (await _db.Releases.ListAsync(f, CancellationToken.None)).Select(r => r.Title).ToArray();

        Assert.Equal(["Future Album", "Today Album", "An EP", "Missing Album", "Incomplete Album", "Undated"], await Titles(DefaultFilter with { ArtistJellyfinId = mine }));
        Assert.Equal(["An EP"], await Titles(DefaultFilter with { Type = ReleaseType.EP }));
        Assert.Equal(["Future Album"], await Titles(DefaultFilter with { State = ListState.Upcoming }));
        Assert.Equal(["Today Album", "Other Artist Album", "Missing Album", "Undated"], await Titles(DefaultFilter with { State = ListState.Missing, Type = ReleaseType.Album }));
        Assert.Equal(["Incomplete Album"], await Titles(DefaultFilter with { State = ListState.Incomplete }));
        Assert.Equal(["An EP", "Other Artist Album", "Missing Album"], await Titles(DefaultFilter with { From = new DateOnly(2020, 6, 1), To = new DateOnly(2020, 7, 1) }));
        Assert.Equal(ListState.Upcoming, (await _db.Releases.ListAsync(DefaultFilter with { State = ListState.Upcoming }, CancellationToken.None)).Single().State);
    }

    [Fact]
    public async Task ListAsync_ArchivedFlagSplitsDecidedRowsFromTheList()
    {
        await Seed("Kept", "2020-01-01", "rg-kept");
        await Seed("Ignored Album", "2020-01-02", "rg-ign");
        var user = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await _db.ExecuteAsync($"INSERT INTO decision (artist_key, normalized_title, kind, user_id, decided_at) VALUES ('name:daft punk', 'ignored album', 'Ignore', '{user}', '2026-09-01T10:00:00Z')");

        var list = await _db.Releases.ListAsync(DefaultFilter, CancellationToken.None);
        var archive = await _db.Releases.ListAsync(DefaultFilter with { Archived = true }, CancellationToken.None);

        Assert.Equal(["Kept"], list.Select(r => r.Title));
        Assert.All(list, r => Assert.Null(r.Archived));
        var archived = Assert.Single(archive);
        Assert.Equal("Ignored Album", archived.Title);
        Assert.Equal((DecisionKind.Ignore, user, new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero)), (archived.Archived!.Kind, archived.Archived.UserId, archived.Archived.DecidedAt));
    }

    private Task SeedManyAsync(int count) => _db.ExecuteAsync($"""
        WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i + 1 FROM n WHERE i < {count})
        INSERT INTO release (library_artist_id, normalized_title, title, canonical_source, canonical_source_id, primary_type, secondary_types, release_date, date_sort, first_seen_at, last_seen_at, ownership_state)
        SELECT {_artist}, 'title ' || i, 'Title ' || i, 'musicbrainz', 'rg-' || i, 'Album', '[]', '2020-01-01', '2020-01-01', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', 'Missing' FROM n;
        INSERT INTO source_entry (release_id, source, source_release_id, url, source_title, last_seen_run_id)
        SELECT id, 'musicbrainz', canonical_source_id, 'https://musicbrainz.org/release-group/' || canonical_source_id, title, 1 FROM release;
        """);

    [Fact]
    public async Task ListAsync_ReturnsAtMostFiveThousandRows()
    {
        await SeedManyAsync(5_001);

        Assert.Equal(5_000, (await _db.Releases.ListAsync(DefaultFilter, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task Editions_UpsertIsUniquePerSourceEditionId_AndOwnershipColumnsAreStored()
    {
        var release = await Seed("Tron Legacy", "2010-12-03", "rg-tron");
        var japan = new EditionTrackList("rel-jp", "Tron: Legacy (Japan)", ["overture", "the grid", "derezzed"]);

        var first = await _db.Releases.UpsertEditionAsync(release, "musicbrainz", japan, Now, CancellationToken.None);
        var again = await _db.Releases.UpsertEditionAsync(release, "musicbrainz", japan with { Title = "Tron: Legacy (Japan, reissue)" }, Now, CancellationToken.None);
        await _db.Releases.UpsertEditionAsync(release, "deezer", new EditionTrackList("dz-tron", "TRON: Legacy", ["overture", "the grid"]), Now, CancellationToken.None);

        Assert.Equal(first, again);
        var editions = await _db.Releases.GetEditionsAsync(release, CancellationToken.None);
        Assert.Equal([("deezer", "dz-tron", "TRON: Legacy", 2), ("musicbrainz", "rel-jp", "Tron: Legacy (Japan, reissue)", 3)], editions.Select(e => (e.Source, e.SourceEditionId, e.Title, e.Tracks.Count)));

        var album = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await _db.Releases.WriteOwnershipAsync(release, new OwnershipResult(OwnershipState.Incomplete, "Title", album, first, ["derezzed"]), Now, CancellationToken.None);

        var stored = (await _db.Releases.GetAsync(release, CancellationToken.None))!;
        Assert.Equal((OwnershipState.Incomplete, "Title", album, first, Now), (stored.OwnershipState, stored.MatchMethod, stored.LibraryAlbumId, stored.ComparedEditionId, stored.OwnershipCheckedAt));
        Assert.Equal(["derezzed"], stored.MissingTracks);
    }

    [Fact]
    public async Task PurgeAsync_EmptiesReleaseDataAndKeepsDecisionsArtistsAndSourceState()
    {
        var release = await Seed("Purged", "2020-01-01", "rg-p");
        await _db.Releases.UpsertEditionAsync(release, "musicbrainz", new EditionTrackList("rel-p", "Purged", ["a"]), Now, CancellationToken.None);
        await _db.Artists.SetMatchAsync(_artist, "musicbrainz", ArtistMatch.Matched("mb-1"), CancellationToken.None);
        await _db.ExecuteAsync("INSERT INTO decision (artist_key, normalized_title, kind, user_id, decided_at) VALUES ('name:daft punk', 'purged', 'Ignore', 'u', '2026-09-01T10:00:00Z')");

        await _db.Releases.PurgeAsync(CancellationToken.None);

        foreach (var emptied in new[] { "release", "source_entry", "edition" })
        {
            Assert.Equal(0L, await _db.ScalarAsync<long>($"SELECT COUNT(*) FROM {emptied}"));
        }

        foreach (var kept in new[] { "decision", "library_artist", "artist_source" })
        {
            Assert.Equal(1L, await _db.ScalarAsync<long>($"SELECT COUNT(*) FROM {kept}"));
        }
    }

    /// <summary>SC-005: the list request completes in under 500 ms. Measured at about 3 ms here, so the criterion itself is the assertion.</summary>
    [Fact]
    public async Task ListAsync_FiveHundredStoredReleases_ListsWithinBudget()
    {
        await SeedManyAsync(500);
        await _db.Releases.ListAsync(DefaultFilter, CancellationToken.None); // warm the connection pool

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var rows = await _db.Releases.ListAsync(DefaultFilter, CancellationToken.None);
        watch.Stop();

        _output.WriteLine($"ListAsync with 500 releases: {watch.ElapsedMilliseconds} ms (SC-005 budget 500 ms)");
        Assert.Equal(500, rows.Count);
        Assert.InRange(watch.ElapsedMilliseconds, 0, 500);
    }
}
