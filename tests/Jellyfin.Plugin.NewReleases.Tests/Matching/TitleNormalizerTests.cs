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
}
