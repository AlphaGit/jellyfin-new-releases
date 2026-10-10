using Jellyfin.Plugin.NewReleases.Sources;

namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>The reason texts a source writes for an Unmatched artist, and the matching steps they stand for (spec 008).</summary>
public static class MatchReasons
{
    public static string LowScore(int score) => $"low score ({score})";

    /// <summary>The step a stored reason says failed, with the numbers it holds; null for a text no released version writes.</summary>
    public static FailedStep? Parse(string source, string? reason)
        => source == SourceLimits.MusicBrainz && reason == "no result" ? new FailedStep("Search", []) : null;

    public static string Ambiguous(int best, int runnerUp) => $"ambiguous (score {best} vs {runnerUp})";
}

/// <summary>The matching step a reason names as failed (spec 008 FR-003) and the values that decided it (FR-004).</summary>
public sealed record FailedStep(string Step, IReadOnlyList<int> Values);
