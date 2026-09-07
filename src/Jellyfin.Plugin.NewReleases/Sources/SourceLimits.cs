namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>Per-source politeness constants (FR-011, research R10). Constants, not configuration: these values never change (constitution VI).</summary>
public static class SourceLimits
{
    public const string MusicBrainz = "musicbrainz";
    public const string Deezer = "deezer";

    public const int FailureThreshold = 5;
    public static readonly TimeSpan Cooldown = TimeSpan.FromHours(6);
    public static readonly TimeSpan[] RetryBackoffs = [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(3_200)];
    public static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds(60);
    public const long MaxResponseBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, (double RequestsPerSecond, int DailyBudget)> BySource =
        new Dictionary<string, (double, int)>(StringComparer.Ordinal)
        {
            [MusicBrainz] = (1, 10_000),
            [Deezer] = (5, 20_000),
        };
}
