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

    [Fact]
    public async Task GetStringAsync_RecordsOneCallPerHttpRequestForThatSource()
    {
        _http.AlwaysReturn(HttpStatusCode.OK, "{}");
        var client = Client();

        await client.GetStringAsync(Deezer, Url, CancellationToken.None);
        await client.GetStringAsync(Deezer, Url, CancellationToken.None);

        var state = await _db.SourceState.GetAsync(Deezer, CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(2, state.CallsToday);
        Assert.Null(await _db.SourceState.GetAsync("musicbrainz", CancellationToken.None));
    }

    private Task SpendBudgetAsync(string source, int calls)
        => _db.ExecuteAsync($"INSERT INTO source_state (source, calls_today, calls_day) VALUES ('{source}', {calls}, '{Start:yyyy-MM-dd}')");

    [Fact]
    public async Task GetStringAsync_LastUnitOfBudgetIsSent_ExhaustedBudgetThrowsWithoutSending()
    {
        _http.AlwaysReturn(HttpStatusCode.OK, "{}");
        var budget = SourceLimits.BySource[Deezer].DailyBudget;
        await SpendBudgetAsync(Deezer, budget - 1);
        var client = Client();

        await client.GetStringAsync(Deezer, Url, CancellationToken.None);
        Assert.Single(_http.ReceivedRequests);

        await Assert.ThrowsAsync<DailyBudgetExhaustedException>(() => client.GetStringAsync(Deezer, Url, CancellationToken.None));
        Assert.Single(_http.ReceivedRequests);
    }

    /// <summary>Drives a task that waits on the stub clock: advance in steps until it completes (bounded).</summary>
    private async Task<T> RunAdvancingAsync<T>(Task<T> task, TimeSpan step, int maxSteps = 20)
    {
        for (var i = 0; i < maxSteps && !task.IsCompleted; i++)
        {
            _clock.Advance(step);
            await Task.Yield();
        }

        return await task;
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string body, int? retryAfterSeconds = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
        if (retryAfterSeconds is { } seconds)
        {
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(seconds));
        }

        return response;
    }

    [Fact]
    public async Task GetStringAsync_503WithRetryAfter_SetsNextAllowedAtWaitsRetriesAndReturnsTheLaterBody()
    {
        _http.OnUrlPattern(".*", (_, attempt) => attempt == 0 ? Response(HttpStatusCode.ServiceUnavailable, "busy", retryAfterSeconds: 2) : Response(HttpStatusCode.OK, "{\"ok\":true}"));
        var client = Client();

        var pending = client.GetStringAsync(Deezer, Url, CancellationToken.None);
        Assert.False(pending.IsCompleted);
        Assert.Equal(Start + TimeSpan.FromSeconds(2), (await _db.SourceState.GetAsync(Deezer, CancellationToken.None))!.NextAllowedAt);

        var body = await RunAdvancingAsync(pending, TimeSpan.FromSeconds(1));

        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(2, _http.ReceivedRequests.Count);
    }
}
