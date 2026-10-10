using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Sources;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Matching;

/// <summary>
/// The reason texts a source writes when it leaves an artist Unmatched, and the matching steps they
/// stand for (spec 008, "The matching steps today"). Every text a released version wrote must parse.
/// </summary>
public class MatchReasonsTests
{
    [Fact]
    public void LowScore_WritesTheBestScoreInParentheses()
    {
        Assert.Equal("low score (70)", MatchReasons.LowScore(70));
    }

    [Fact]
    public void Ambiguous_WritesTheBestScoreThenTheRunnerUp()
    {
        Assert.Equal("ambiguous (score 92 vs 90)", MatchReasons.Ambiguous(92, 90));
    }

    [Fact]
    public void Parse_MusicBrainzNoResult_FailsAtSearchWithNoValues()
    {
        var failed = MatchReasons.Parse(SourceLimits.MusicBrainz, "no result");

        Assert.Equal("Search", failed?.Step);
        Assert.Empty(failed!.Values);
    }
}
