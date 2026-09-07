using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Storage;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>
/// The one way out to a source (FR-011, FR-018): per-source token bucket, daily budget, cooldown, `Retry-After`
/// backoff and the identifying `User-Agent`. Sources never touch <see cref="HttpClient"/> directly.
/// </summary>
public sealed class SourceHttpClient
{
    public const string ClientName = "newreleases";

    /// <summary>Plugin version advertised in the User-Agent (three fields, from the assembly).</summary>
    public static readonly string Version = typeof(SourceHttpClient).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private readonly IHttpClientFactory _factory;
    private readonly SourceStateRepository _state;
    private readonly TimeProvider _clock;
    private readonly Func<PluginConfiguration> _configuration;
    private readonly ILogger<SourceHttpClient> _logger;

    public SourceHttpClient(IHttpClientFactory factory, SourceStateRepository state, TimeProvider clock, ILogger<SourceHttpClient> logger)
        : this(factory, state, clock, logger, () => Plugin.Instance?.Configuration ?? new PluginConfiguration())
    {
    }

    public SourceHttpClient(IHttpClientFactory factory, SourceStateRepository state, TimeProvider clock, ILogger<SourceHttpClient> logger, Func<PluginConfiguration> configuration)
    {
        _factory = factory;
        _state = state;
        _clock = clock;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> GetStringAsync(string source, string url, CancellationToken ct)
    {
        var limits = SourceLimits.BySource[source];
        if (await _state.GetRemainingBudgetAsync(source, limits.DailyBudget, ct).ConfigureAwait(false) <= 0)
        {
            throw new DailyBudgetExhaustedException(source);
        }

        using var client = _factory.CreateClient(ClientName);
        for (var attempt = 0; ; attempt++)
        {
            await WaitForBackoffFloorAsync(source, ct).ConfigureAwait(false);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgentBuilder.Build(Version, _configuration().UserAgentContact));
            await _state.RecordCallAsync(source, ct).ConfigureAwait(false);
            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }

            var transient = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            if (!transient)
            {
                throw new HttpRequestException($"Source '{source}' answered {(int)response.StatusCode}.", null, response.StatusCode);
            }

            if (attempt >= SourceLimits.RetryBackoffs.Length)
            {
                throw new HttpRequestException($"Source '{source}' still answered {(int)response.StatusCode} after {attempt} retries.", null, response.StatusCode);
            }

            var retryAfter = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date is { } date ? date - _clock.GetUtcNow() : null);
            var delay = retryAfter ?? SourceLimits.RetryBackoffs[attempt];
            if (retryAfter is not null)
            {
                await _state.SetNextAllowedAtAsync(source, _clock.GetUtcNow() + delay, ct).ConfigureAwait(false);
            }

            _logger.LogWarning("Source {Source} answered {Status}; retrying in {Delay}.", source, (int)response.StatusCode, delay);
            await Task.Delay(delay, _clock, ct).ConfigureAwait(false);
        }
    }

    private async Task WaitForBackoffFloorAsync(string source, CancellationToken ct)
    {
        var state = await _state.GetAsync(source, ct).ConfigureAwait(false);
        if (state?.NextAllowedAt is { } floor && floor > _clock.GetUtcNow())
        {
            await Task.Delay(floor - _clock.GetUtcNow(), _clock, ct).ConfigureAwait(false);
        }
    }
}

/// <summary>Thrown before sending when the source's daily budget is spent (FR-011). The caller records a Partial outcome.</summary>
public sealed class DailyBudgetExhaustedException(string source) : Exception($"Daily request budget for source '{source}' is exhausted.")
{
    public string SourceId { get; } = source;
}
