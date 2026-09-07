using Jellyfin.Plugin.NewReleases.Api;
using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Api;

/// <summary>Controller actions invoked directly with a real temp database; the ASP.NET pipeline (routing, `[Authorize]`) is not in the loop.</summary>
public sealed class ReleasesControllerTests : IAsyncLifetime
{
    private static readonly Guid Library = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Alice = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    private readonly TimeProviderStub _clock = new(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly PluginConfiguration _configuration = new();
    private readonly ITaskManager _tasks = Substitute.For<ITaskManager>();
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync(_clock);

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private ReleasesController Controller(Guid? caller, params Jellyfin.Database.Implementations.Entities.User[] users)
        => new(_db.Releases, _db.Artists, _db.Archive, _db.SourceState, ControllerContextFactory.UserManager(users), _tasks, _clock, NullLogger<ReleasesController>.Instance, () => _configuration)
        {
            ControllerContext = ControllerContextFactory.ForUser(caller),
        };

    [Fact]
    public async Task GetReleases_WithoutTheUserIdClaim_Is401()
    {
        var result = await Controller(caller: null).GetReleasesAsync(cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    private static readonly Guid OtherLibrary = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Seeded = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Stores one artist in the given library with one Missing release per title (via the real repositories).</summary>
    private async Task<Guid> SeedArtistAsync(string name, Guid library, params (string Title, string? Date)[] releases)
    {
        var jellyfinId = Guid.NewGuid();
        var id = await _db.Artists.UpsertAsync(new LibraryArtistSnapshot("name:" + name.ToLowerInvariant(), jellyfinId, name, null, [library], []), CancellationToken.None);
        foreach (var (title, date) in releases)
        {
            var item = new CatalogueItem("rg-" + title.ToLowerInvariant().Replace(' ', '-'), title, "https://musicbrainz.org/release-group/x", ReleaseType.Album, [], date);
            await _db.Releases.UpsertFromSourceAsync(id, "musicbrainz", item, 1, Seeded, CancellationToken.None);
        }

        return jellyfinId;
    }

    private static ListResponse Ok(ActionResult<ListResponse> result) => result.Value ?? (ListResponse)((ObjectResult)result.Result!).Value!;

    [Fact]
    public async Task GetReleases_FollowsTheCallersLibraryAccess()
    {
        await SeedArtistAsync("Daft Punk", Library, ("Discovery", "2001-03-12"));
        await SeedArtistAsync("Justice", OtherLibrary, ("Cross", "2007-06-11"));

        var everything = Ok(await Controller(Alice, ControllerContextFactory.User(Alice, allFolders: true)).GetReleasesAsync(cancellationToken: CancellationToken.None));
        var none = Ok(await Controller(Alice, ControllerContextFactory.User(Alice, allFolders: false, OtherLibrary)).GetReleasesAsync(cancellationToken: CancellationToken.None));
        var some = Ok(await Controller(Alice, ControllerContextFactory.User(Alice, allFolders: false, Library)).GetReleasesAsync(cancellationToken: CancellationToken.None));

        Assert.Equal(["Cross", "Discovery"], everything.Items.Select(i => i.Title));
        Assert.Equal(["Cross"], none.Items.Select(i => i.Title));
        Assert.Equal(["Discovery"], some.Items.Select(i => i.Title));
        Assert.Equal(1, some.Total);
    }

    [Fact]
    public async Task GetReleases_PassesFromTypeStateAndArtistToTheFilter()
    {
        var daftPunk = await SeedArtistAsync("Daft Punk", Library, ("On The Day", "2020-01-01"), ("Day Before", "2019-12-31"), ("Future One", "2027-01-01"));
        await SeedArtistAsync("Justice", Library, ("Cross", "2007-06-11"));
        await _db.ExecuteAsync("UPDATE release SET primary_type = 'EP' WHERE title = 'Cross'");
        var controller = Controller(Alice, ControllerContextFactory.User(Alice, allFolders: true));

        Assert.Equal(["Future One", "On The Day"], Ok(await controller.GetReleasesAsync(from: "2020-01-01", cancellationToken: CancellationToken.None)).Items.Select(i => i.Title));
        Assert.Equal(["Cross"], Ok(await controller.GetReleasesAsync(type: "EP", cancellationToken: CancellationToken.None)).Items.Select(i => i.Title));
        Assert.Equal(["Future One"], Ok(await controller.GetReleasesAsync(state: "Upcoming", cancellationToken: CancellationToken.None)).Items.Select(i => i.Title));
        Assert.Equal(["Future One", "On The Day", "Day Before"], Ok(await controller.GetReleasesAsync(artistId: daftPunk, cancellationToken: CancellationToken.None)).Items.Select(i => i.Title));
    }

    private void TriggersAre(params TaskTriggerInfo[] triggers)
    {
        var worker = Substitute.For<IScheduledTaskWorker>();
        worker.ScheduledTask.Returns(new RefreshNewReleasesTask(null!, null!, null!, null!, null!, [], null!, null!)); // only its type/key matter here
        worker.Triggers.Returns(triggers);
        _tasks.ScheduledTasks.Returns([worker]);
    }

    [Fact]
    public async Task GetReleases_LastRefreshedAtIsTheLastCompletedRunsEnd_RefreshIntervalFollowsTheTrigger()
    {
        var controller = Controller(Alice, ControllerContextFactory.User(Alice, allFolders: true));
        Assert.Equal((false, (DateTimeOffset?)null), (Ok(await controller.GetReleasesAsync(cancellationToken: CancellationToken.None)).HasCompletedRefresh, Ok(await controller.GetReleasesAsync(cancellationToken: CancellationToken.None)).LastRefreshedAt));

        var run = await _db.SourceState.StartRunAsync(CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(5));
        await _db.SourceState.FinishRunAsync(run, 1, 1, 0, 0, "Completed", CancellationToken.None);
        var endedAt = _clock.GetUtcNow();

        TriggersAre(new TaskTriggerInfo { Type = TaskTriggerInfoType.DailyTrigger, TimeOfDayTicks = TimeSpan.FromHours(3).Ticks });
        var daily = Ok(await controller.GetReleasesAsync(cancellationToken: CancellationToken.None));
        Assert.Equal((true, endedAt, 24), (daily.HasCompletedRefresh, daily.LastRefreshedAt, daily.RefreshIntervalHours));

        TriggersAre(new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(12).Ticks });
        Assert.Equal(12, Ok(await controller.GetReleasesAsync(cancellationToken: CancellationToken.None)).RefreshIntervalHours);

        _tasks.ScheduledTasks.Returns([]);
        Assert.Equal(24, Ok(await controller.GetReleasesAsync(cancellationToken: CancellationToken.None)).RefreshIntervalHours);
    }

    [Fact]
    public async Task GetArtists_ReturnsOnlyArtistsInLibrariesTheCallerMayAccess()
    {
        await SeedArtistAsync("Daft Punk", Library);
        await SeedArtistAsync("Justice", OtherLibrary);

        var limited = await Controller(Alice, ControllerContextFactory.User(Alice, allFolders: false, Library)).GetArtistsAsync(CancellationToken.None);
        var all = await Controller(Alice, ControllerContextFactory.User(Alice, allFolders: true)).GetArtistsAsync(CancellationToken.None);

        Assert.Equal(["Daft Punk"], limited.Value!.Items.Select(a => a.Name));
        Assert.Equal(["Daft Punk", "Justice"], all.Value!.Items.Select(a => a.Name));
        Assert.IsType<UnauthorizedResult>((await Controller(null).GetArtistsAsync(CancellationToken.None)).Result);
    }
}
