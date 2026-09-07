using System.Net;
using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Sources;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>Everything a source or the task needs to talk to a stubbed network: stub handler, temp database, stub clock, real <see cref="SourceHttpClient"/>.</summary>
internal sealed class SourceHarness : IAsyncDisposable
{
    public static readonly DateTimeOffset Start = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    private SourceHarness(TestDatabase db)
    {
        Db = db;
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(SourceHttpClient.ClientName).Returns(_ => new HttpClient(Http, disposeHandler: false));
        HttpClient = new SourceHttpClient(factory, db.SourceState, Clock, NullLogger<SourceHttpClient>.Instance, () => Configuration);
    }

    public TimeProviderStub Clock { get; } = new(Start);

    public StubHttpMessageHandler Http { get; } = new();

    public PluginConfiguration Configuration { get; } = new();

    public TestDatabase Db { get; }

    public SourceHttpClient HttpClient { get; }

    public static async Task<SourceHarness> CreateAsync()
    {
        var clock = new TimeProviderStub(Start);
        return new SourceHarness(await TestDatabase.CreateAsync(clock));
    }

    /// <summary>Serves a recorded fixture (path under <c>tests/fixtures/</c>) for URLs matching the pattern.</summary>
    public SourceHarness Fixture(string urlPattern, string fixturePath, HttpStatusCode status = HttpStatusCode.OK)
    {
        Http.OnUrlPattern(urlPattern, status, FixtureLoader.LoadText(fixturePath));
        return this;
    }

    public IReadOnlyList<string> RequestedUrls => Http.ReceivedRequests.Select(r => r.RequestUri!.ToString()).ToList();

    public async ValueTask DisposeAsync()
    {
        await HttpClient.DisposeAsync();
        await Db.DisposeAsync();
    }
}
