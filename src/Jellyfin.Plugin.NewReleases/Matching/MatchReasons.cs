namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>The reason texts a source writes for an Unmatched artist, and the matching steps they stand for (spec 008).</summary>
public static class MatchReasons
{
    public static string LowScore(int score) => $"low score ({score})";
}
