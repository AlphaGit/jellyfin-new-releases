using System.Net;
using Jellyfin.Plugin.NewReleases.Api;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.NewReleases.Tests.Acceptance;

/// <summary>
/// The real entry points composed as the server would: `RefreshNewReleasesTask.ExecuteAsync` with the real sources over a
/// stubbed network, then `ReleasesController` actions as a Jellyfin user. Only Jellyfin services and HTTP are substituted.
/// </summary>
internal sealed class AcceptanceRig : IAsyncDisposable
{
    public static readonly Guid Library = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Alice = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    private AcceptanceRig(SourceHarness harness)
    {
        Harness = harness;
        Task = new RefreshNewReleasesTask(
            new LibraryScanner(Library_.Manager, NullLogger<LibraryScanner>.Instance),
            harness.Db.Artists, harness.Db.Releases, harness.Db.SourceState, harness.HttpClient,
            [new MusicBrainzSource(harness.HttpClient, NullLogger<MusicBrainzSource>.Instance), new DeezerSource(harness.HttpClient, harness.Clock, NullLogger<DeezerSource>.Instance)],
            harness.Clock, NullLogger<RefreshNewReleasesTask>.Instance, () => harness.Configuration);
    }

    public SourceHarness Harness { get; }

    public LibraryFakes Library_ { get; } = new();

    public ITaskManager Tasks { get; } = Substitute.For<ITaskManager>();

    public RefreshNewReleasesTask Task { get; }

    public static async Task<AcceptanceRig> CreateAsync() => new(await SourceHarness.CreateAsync());

    public Task RunAsync(CancellationToken ct = default) => Task.ExecuteAsync(new Progress<double>(), ct);

    public ReleasesController ControllerFor(Guid user, bool allFolders = true, params Guid[] folders) => new(
        Harness.Db.Releases, Harness.Db.Artists, Harness.Db.Archive, Harness.Db.SourceState,
        ControllerContextFactory.UserManager(ControllerContextFactory.User(user, allFolders, folders)), Tasks, Harness.Clock,
        NullLogger<ReleasesController>.Instance, () => Harness.Configuration)
    {
        ControllerContext = ControllerContextFactory.ForUser(user),
    };

    public AdminController AdminController() => new(
        Harness.Db.Artists, Harness.Db.Releases, Harness.Db.Archive, Harness.Db.SourceState, Tasks, Harness.Clock,
        NullLogger<AdminController>.Instance, () => Harness.Configuration);

    public async Task<AdminStatusResponse> AdminStatusAsync()
    {
        var result = await AdminController().GetStatusAsync(CancellationToken.None);
        return result.Value ?? (AdminStatusResponse)((ObjectResult)result.Result!).Value!;
    }

    public async Task<ListResponse> ListAsync(Guid? artistId = null, string? type = null, string? state = null, string? from = null, string? to = null, bool archived = false)
    {
        var result = await ControllerFor(Alice).GetReleasesAsync(artistId, type, state, from, to, archived, CancellationToken.None);
        return result.Value ?? (ListResponse)((ObjectResult)result.Result!).Value!;
    }

    // ── Deezer scenario wiring (fast: 5 req/s) ────────────────────────────────

    public AcceptanceRig DeezerArtist(long id, string name, params (long Id, string Title, string RecordType, string Date)[] albums)
    {
        // The stub matches on Uri.ToString(), which unescapes %20 back to a space.
        Harness.Http.OnUrlPattern(@"api\.deezer\.com/search/artist\?q=" + System.Text.RegularExpressions.Regex.Escape(name) + "&", HttpStatusCode.OK, SourceJson.Deezer.Search((id, name)));
        Harness.Http.OnUrlPattern($@"api\.deezer\.com/artist/{id}/albums\?", HttpStatusCode.OK, SourceJson.Deezer.Albums(albums));
        return this;
    }

    public AcceptanceRig DeezerAlbum(long id, string title, params string[] tracks)
    {
        Harness.Http.OnUrlPattern($@"api\.deezer\.com/album/{id}$", HttpStatusCode.OK, SourceJson.Deezer.Album(id, title));
        Harness.Http.OnUrlPattern($@"api\.deezer\.com/album/{id}/tracks\?", HttpStatusCode.OK, SourceJson.Deezer.Tracks(tracks));
        return this;
    }

    // ── MusicBrainz scenario wiring (1 req/s: use sparingly) ──────────────────

    public AcceptanceRig MusicBrainzCatalogue(string mbid, params (string GroupId, string Title, string Primary, string[] Secondaries, string? Date)[] groups)
    {
        Harness.Http.OnUrlPattern($@"musicbrainz\.org/ws/2/release\?artist={mbid}", HttpStatusCode.OK, SourceJson.MusicBrainz.Releases(groups));
        return this;
    }

    public AcceptanceRig MusicBrainzEditions(string groupId, params (string ReleaseId, string Title, string[] Tracks)[] editions)
    {
        Harness.Http.OnUrlPattern($@"musicbrainz\.org/ws/2/release\?release-group={groupId}", HttpStatusCode.OK, SourceJson.MusicBrainz.Editions(editions));
        return this;
    }

    public ValueTask DisposeAsync() => Harness.DisposeAsync();
}
