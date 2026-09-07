using System.Net;
using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Sources;

public sealed class SourceHttpClientTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);
    private const string Deezer = "deezer";
    private const string Url = "https://api.deezer.com/artist/27/albums?index=0&limit=100";

    private readonly TimeProviderStub _clock = new(Start);
    private readonly StubHttpMessageHandler _http = new();
    private readonly PluginConfiguration _configuration = new();
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync(_clock);

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private SourceHttpClient Client()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(SourceHttpClient.ClientName).Returns(_ => new HttpClient(_http, disposeHandler: false));
        return new SourceHttpClient(factory, _db.SourceState, _clock, NullLogger<SourceHttpClient>.Instance, () => _configuration);
    }

    [Fact]
    public async Task GetStringAsync_EveryRequestCarriesThePluginUserAgent()
    {
        _configuration.UserAgentContact = "ops@example.org";
        _http.AlwaysReturn(HttpStatusCode.OK, "{}");

        await Client().GetStringAsync(Deezer, Url, CancellationToken.None);

        var request = Assert.Single(_http.ReceivedRequests);
        Assert.Equal($"JellyfinNewReleases/{typeof(Plugin).Assembly.GetName().Version!.ToString(3)} ( ops@example.org )", request.Headers.UserAgent.ToString());
    }
}
