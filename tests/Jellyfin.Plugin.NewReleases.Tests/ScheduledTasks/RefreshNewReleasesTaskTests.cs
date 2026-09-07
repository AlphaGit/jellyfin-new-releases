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
}
