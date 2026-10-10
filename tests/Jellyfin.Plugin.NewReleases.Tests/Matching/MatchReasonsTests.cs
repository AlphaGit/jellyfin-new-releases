using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
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

    [Fact]
    public void Parse_MusicBrainzLowScore_FailsAtScoreWithTheScore()
    {
        var failed = MatchReasons.Parse(SourceLimits.MusicBrainz, "low score (70)");

        Assert.Equal("Score", failed?.Step);
        Assert.Equal([70], failed!.Values);
    }

    [Fact]
    public void Parse_MusicBrainzAmbiguous_FailsAtUniqueWithBothScores()
    {
        var failed = MatchReasons.Parse(SourceLimits.MusicBrainz, "ambiguous (score 92 vs 90)");

        Assert.Equal("Unique", failed?.Step);
        Assert.Equal([92, 90], failed!.Values);
    }

    [Fact]
    public void Parse_DeezerNoResult_FailsAtNameWithNoValues()
    {
        var failed = MatchReasons.Parse(SourceLimits.Deezer, "no result");

        Assert.Equal("Name", failed?.Step);
        Assert.Empty(failed!.Values);
    }

    [Fact]
    public void Parse_DeezerNoCorroboratingAlbum_FailsAtAlbumWithNoValues()
    {
        var failed = MatchReasons.Parse(SourceLimits.Deezer, "no corroborating album");

        Assert.Equal("Album", failed?.Step);
        Assert.Empty(failed!.Values);
    }

    [Theory]
    [InlineData(SourceLimits.MusicBrainz)]
    [InlineData(SourceLimits.Deezer)]
    public void Parse_ATextNoReleasedVersionWrites_IsNull(string source)
    {
        Assert.Null(MatchReasons.Parse(source, "rate limited"));
    }

    [Theory]
    [InlineData(SourceLimits.MusicBrainz)]
    [InlineData(SourceLimits.Deezer)]
    public void Parse_AnEmptyText_IsNull(string source)
    {
        Assert.Null(MatchReasons.Parse(source, string.Empty));
    }

    [Fact]
    public void Parse_AMusicBrainzOnlyTextUnderDeezer_IsNull()
    {
        Assert.Null(MatchReasons.Parse(SourceLimits.Deezer, "low score (70)"));
    }

    [Fact]
    public void Steps_MusicBrainzFailedAtSearch_IdAndSearchFailed_TheRestNotReached()
    {
        var steps = MatchReasons.Steps(SourceLimits.MusicBrainz, new FailedStep("Search", []));

        Assert.Equal(
            [("Id", StepState.Failed), ("Search", StepState.Failed), ("Score", StepState.NotReached), ("Unique", StepState.NotReached)],
            steps.Select(s => (s.Name, s.State)));
    }

    [Fact]
    public void Steps_MusicBrainzFailedAtScore_CarriesTheScoreOnTheScoreStepOnly()
    {
        var steps = MatchReasons.Steps(SourceLimits.MusicBrainz, new FailedStep("Score", [70]));

        Assert.Equal(
            [("Id", StepState.Failed, ""), ("Search", StepState.Passed, ""), ("Score", StepState.Failed, "70"), ("Unique", StepState.NotReached, "")],
            steps.Select(s => (s.Name, s.State, string.Join(' ', s.Values))));
    }
}
