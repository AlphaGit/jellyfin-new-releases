using Jellyfin.Plugin.NewReleases.Matching;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Matching;

/// <summary>
/// Rules from <c>docs/domain_knowledge/title-normalization.md</c>. Titles match only when equal
/// after normalization, so every rule is pinned by an example from that document.
/// </summary>
public class TitleNormalizerTests
{
    [Theory]
    [InlineData("Café Bleu", "cafe bleu")]
    [InlineData("Mø", "mo")]
    public void NormalizeAlbum_StripsDiacritics(string input, string expected)
    {
        Assert.Equal(expected, TitleNormalizer.NormalizeAlbum(input));
    }

    [Fact]
    public void NormalizeAlbum_ReplacesAmpersandWithAnd()
    {
        Assert.Equal("rock and roll", TitleNormalizer.NormalizeAlbum("Rock & Roll"));
    }

    [Fact]
    public void NormalizeAlbum_RemovesPunctuationAndCollapsesWhitespace()
    {
        Assert.Equal("ab c", TitleNormalizer.NormalizeAlbum("A.B.  --  C!"));
    }

    [Theory]
    [InlineData("TANZNEID (24-bit HD audio)", "tanzneid")]
    [InlineData("Random Access Memories [Explicit]", "random access memories")]
    [InlineData("Album - 10th Anniversary Edition", "album")]
    public void NormalizeAlbum_RemovesTrailingEditionQualifier(string input, string expected)
    {
        Assert.Equal(expected, TitleNormalizer.NormalizeAlbum(input));
    }

    [Theory]
    [InlineData("X (Deluxe) (Remastered)", "x deluxe")]
    [InlineData("Deluxe Edition Blues", "deluxe edition blues")]
    public void NormalizeAlbum_RemovesAtMostOneQualifierFromTheEndOnly(string input, string expected)
    {
        Assert.Equal(expected, TitleNormalizer.NormalizeAlbum(input));
    }

    [Theory]
    [InlineData("Get Lucky (feat. Pharrell Williams)", "get lucky")]
    [InlineData("Get Lucky ft. Pharrell", "get lucky")]
    public void NormalizeTrack_RemovesTrailingFeaturedArtist(string input, string expected)
    {
        Assert.Equal(expected, TitleNormalizer.NormalizeTrack(input));
    }

    [Fact]
    public void NormalizeTrack_KeepsEditionQualifier()
    {
        Assert.Equal("song deluxe", TitleNormalizer.NormalizeTrack("Song (Deluxe)"));
    }

    [Theory]
    [InlineData("The Album", "Album")]
    [InlineData("Vol. 2", "Volume 2")]
    public void NormalizeAlbum_KeepsArticlesAndAbbreviationsDistinct(string left, string right)
    {
        Assert.NotEqual(TitleNormalizer.NormalizeAlbum(left), TitleNormalizer.NormalizeAlbum(right));
    }
}
