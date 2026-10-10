using System.Globalization;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.NewReleases.Sources;

namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>The reason texts a source writes for an Unmatched artist, and the matching steps they stand for (spec 008).</summary>
public static partial class MatchReasons
{
    public static string LowScore(int score) => $"low score ({score})";

    public static string Ambiguous(int best, int runnerUp) => $"ambiguous (score {best} vs {runnerUp})";

    /// <summary>The step a stored reason says failed, with the numbers it holds; null for a text no released version writes.</summary>
    public static FailedStep? Parse(string source, string? reason)
    {
        if (reason is null)
        {
            return null;
        }

        if (source == SourceLimits.Deezer)
        {
            return reason switch
            {
                "no result" => new FailedStep("Name", []),
                "no corroborating album" => new FailedStep("Album", []),
                _ => null,
            };
        }

        if (source != SourceLimits.MusicBrainz)
        {
            return null;
        }

        if (reason == "no result")
        {
            return new FailedStep("Search", []);
        }

        var lowScore = LowScorePattern().Match(reason);
        if (lowScore.Success)
        {
            return new FailedStep("Score", [Number(lowScore, 1)]);
        }

        var ambiguous = AmbiguousPattern().Match(reason);
        return ambiguous.Success
            ? new FailedStep("Unique", [Number(ambiguous, 1), Number(ambiguous, 2)])
            : null;
    }

    private static int Number(Match match, int group) => int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^low score \((\d+)\)$")]
    private static partial Regex LowScorePattern();

    [GeneratedRegex(@"^ambiguous \(score (\d+) vs (\d+)\)$")]
    private static partial Regex AmbiguousPattern();
}

/// <summary>The matching step a reason names as failed (spec 008 FR-003) and the values that decided it (FR-004).</summary>
public sealed record FailedStep(string Step, IReadOnlyList<int> Values);
