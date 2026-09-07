using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.ScheduledTasks;

/// <summary>
/// Orchestration behaviours of the Refresh with the two sources substituted at the <see cref="IReleaseSource"/> boundary;
/// storage, clock and the HTTP policy client are real (temp SQLite, stub clock). End-to-end runs with fixtures live in Acceptance/.
/// </summary>
public sealed class RefreshNewReleasesTaskTests : IAsyncLifetime
{
    private static readonly Guid Library = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private SourceHarness _h = null!;
    private readonly LibraryFakes _library = new();
    private readonly PluginConfiguration _configuration = new();
    private readonly IReleaseSource _musicBrainz = Substitute.For<IReleaseSource>();
    private readonly IReleaseSource _deezer = Substitute.For<IReleaseSource>();

    public async Task InitializeAsync()
    {
        _h = await SourceHarness.CreateAsync();
        _musicBrainz.Id.Returns("musicbrainz");
        _musicBrainz.DisplayName.Returns("MusicBrainz");
        _deezer.Id.Returns("deezer");
        _deezer.DisplayName.Returns("Deezer");
    }

    public async Task DisposeAsync() => await _h.DisposeAsync();

    private RefreshNewReleasesTask Task_() => new(
        new LibraryScanner(_library.Manager, NullLogger<LibraryScanner>.Instance),
        _h.Db.Artists,
        _h.Db.Releases,
        _h.Db.SourceState,
        _h.HttpClient,
        [_musicBrainz, _deezer],
        _h.Clock,
        NullLogger<RefreshNewReleasesTask>.Instance,
        () => _configuration);

    [Fact]
    public void Metadata_NameCategoryKeyAndDailyTriggerAtThree()
    {
        var task = Task_();

        Assert.Equal(("Refresh new releases", "New Releases", "NewReleases.Refresh"), (task.Name, task.Category, task.Key));
        var trigger = Assert.Single(task.GetDefaultTriggers());
        Assert.Equal((TaskTriggerInfoType.DailyTrigger, TimeSpan.FromHours(3).Ticks), (trigger.Type, trigger.TimeOfDayTicks));
    }

    private static CataloguePage EmptyPage => new([], null, 0);

    private static CatalogueItem Item(string source, string id, string title, string? date = "2001-03-12", ReleaseType primary = ReleaseType.Album)
        => new(id, title, source == "musicbrainz" ? $"https://musicbrainz.org/release-group/{id}" : $"https://www.deezer.com/album/{id}", primary, [], date);

    private void MatchEverything(IReleaseSource source, string prefix)
    {
        source.MatchArtistAsync(Arg.Any<LibraryArtistSnapshot>(), Arg.Any<CancellationToken>())
            .Returns(call => ArtistMatch.Matched(prefix + call.Arg<LibraryArtistSnapshot>().Name));
        source.FetchCataloguePageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(EmptyPage);
        source.FetchEditionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private Task RunAsync() => Task_().ExecuteAsync(new Progress<double>(), CancellationToken.None);

    [Fact]
    public async Task Run_ProcessesArtistsInRotationOrder_AdvancesLastRefreshedOnlyWhenEverySourceWasAttempted()
    {
        _library.Artist("Recent"); _library.Album("R1", "Recent", Library);
        _library.Artist("Never"); _library.Album("N1", "Never", Library);
        MatchEverything(_musicBrainz, "mb:"); MatchEverything(_deezer, "dz:");
        await _h.Db.Artists.UpsertAsync(new LibraryArtistSnapshot("name:recent", Guid.NewGuid(), "Recent", null, [Library], []), CancellationToken.None);
        await _h.Db.ExecuteAsync("UPDATE library_artist SET last_refreshed_at = '2026-01-01T00:00:00Z'");
        await _h.Db.SourceState.SetNextAllowedAtAsync("deezer", SourceHarness.Start + TimeSpan.FromHours(1), CancellationToken.None); // Deezer unavailable this run

        await RunAsync();

        Received.InOrder(() =>
        {
            _musicBrainz.MatchArtistAsync(Arg.Is<LibraryArtistSnapshot>(a => a.Name == "Never"), Arg.Any<CancellationToken>());
            _musicBrainz.MatchArtistAsync(Arg.Is<LibraryArtistSnapshot>(a => a.Name == "Recent"), Arg.Any<CancellationToken>());
        });
        var afterFirst = await _h.Db.Artists.GetAllAsync(CancellationToken.None);
        Assert.Null(afterFirst.Single(a => a.Name == "Never").LastRefreshedAt);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), afterFirst.Single(a => a.Name == "Recent").LastRefreshedAt);

        _h.Clock.Advance(TimeSpan.FromHours(2)); // Deezer available again
        await RunAsync();

        Assert.All(await _h.Db.Artists.GetAllAsync(CancellationToken.None), a => Assert.Equal(_h.Clock.GetUtcNow(), a.LastRefreshedAt));
    }

    private async Task<string[]> StoredTitlesAsync() => (await _h.Db.ColumnAsync<string>("SELECT title FROM release ORDER BY title")).ToArray();

    [Fact]
    public async Task Run_CompleteFetch_RemovesEntriesThePageSetNoLongerContains_AndOrphanReleases()
    {
        _library.Artist("Daft Punk"); _library.Album("Homework", "Daft Punk", Library);
        _configuration.DeezerEnabled = false;
        MatchEverything(_musicBrainz, "mb:");
        _musicBrainz.FetchCataloguePageAsync(Arg.Any<string>(), 0, Arg.Any<CancellationToken>())
            .Returns(new CataloguePage([Item("musicbrainz", "rg-1", "Discovery"), Item("musicbrainz", "rg-2", "Alive 1997")], null, 2));
        await RunAsync();
        Assert.Equal(["Alive 1997", "Discovery"], await StoredTitlesAsync());

        // Run 2: MusicBrainz no longer lists Alive 1997.
        _musicBrainz.FetchCataloguePageAsync(Arg.Any<string>(), 0, Arg.Any<CancellationToken>())
            .Returns(new CataloguePage([Item("musicbrainz", "rg-1", "Discovery")], null, 1));
        await RunAsync();

        Assert.Equal(["Discovery"], await StoredTitlesAsync());
        Assert.Equal(1L, await _h.Db.ScalarAsync<long>("SELECT COUNT(*) FROM source_entry"));
    }

    [Fact]
    public async Task Run_BudgetExhaustedAfterPageOneOfTwo_IsPartialKeepsOffsetRemovesNothing_NextRunResumesAtOffset()
    {
        _library.Artist("Daft Punk"); _library.Album("Homework", "Daft Punk", Library);
        _configuration.DeezerEnabled = false;
        MatchEverything(_musicBrainz, "mb:");
        var seeded = await _h.Db.Artists.UpsertAsync(new LibraryArtistSnapshot("name:daft punk", Guid.NewGuid(), "Daft Punk", null, [Library], []), CancellationToken.None);
        await _h.Db.Releases.UpsertFromSourceAsync(seeded, "musicbrainz", Item("musicbrainz", "rg-old", "From An Earlier Run"), 0, SourceHarness.Start, CancellationToken.None);
        _musicBrainz.FetchCataloguePageAsync(Arg.Any<string>(), 0, Arg.Any<CancellationToken>())
            .Returns(new CataloguePage([Item("musicbrainz", "rg-1", "Discovery")], 100, 150));
        _musicBrainz.FetchCataloguePageAsync(Arg.Any<string>(), 100, Arg.Any<CancellationToken>())
            .Returns<CataloguePage>(_ => throw new DailyBudgetExhaustedException("musicbrainz"));

        await RunAsync();

        var state = (await _h.Db.Artists.GetSourceStateAsync(seeded, "musicbrainz", CancellationToken.None))!;
        Assert.Equal((FetchOutcome.Partial, 100), (state.LastOutcome, state.ResumeOffset));
        Assert.Equal(["Discovery", "From An Earlier Run"], await StoredTitlesAsync());

        // Next run: budget back, page 2 is the last one.
        _musicBrainz.FetchCataloguePageAsync(Arg.Any<string>(), 100, Arg.Any<CancellationToken>())
            .Returns(new CataloguePage([Item("musicbrainz", "rg-2", "Alive 1997")], null, 150));
        await RunAsync();

        Assert.Equal(1, _musicBrainz.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IReleaseSource.FetchCataloguePageAsync) && (int)c.GetArguments()[1]! == 0));
        Assert.Equal(FetchOutcome.Complete, (await _h.Db.Artists.GetSourceStateAsync(seeded, "musicbrainz", CancellationToken.None))!.LastOutcome);
        Assert.Equal(["Alive 1997", "Discovery"], await StoredTitlesAsync()); // the pre-existing entry was pruned only by the Complete run
    }

    [Fact]
    public async Task Run_FetchThatThrows_IsFailedWithLastErrorAndRemovesNothing()
    {
        _library.Artist("Daft Punk"); _library.Album("Homework", "Daft Punk", Library);
        _configuration.DeezerEnabled = false;
        MatchEverything(_musicBrainz, "mb:");
        var seeded = await _h.Db.Artists.UpsertAsync(new LibraryArtistSnapshot("name:daft punk", Guid.NewGuid(), "Daft Punk", null, [Library], []), CancellationToken.None);
        await _h.Db.Releases.UpsertFromSourceAsync(seeded, "musicbrainz", Item("musicbrainz", "rg-old", "From An Earlier Run"), 0, SourceHarness.Start, CancellationToken.None);
        _musicBrainz.FetchCataloguePageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<CataloguePage>(_ => throw new HttpRequestException("Source 'musicbrainz' still answered 503 after 3 retries."));

        await RunAsync();

        var state = (await _h.Db.Artists.GetSourceStateAsync(seeded, "musicbrainz", CancellationToken.None))!;
        Assert.Equal(FetchOutcome.Failed, state.LastOutcome);
        Assert.Contains("503", state.LastError);
        Assert.Equal(["From An Earlier Run"], await StoredTitlesAsync());
    }
}
