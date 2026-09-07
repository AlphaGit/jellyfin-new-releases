using System.Globalization;
using System.Text;

namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>
/// Deterministic title normalization from <c>docs/domain_knowledge/title-normalization.md</c>.
/// Two titles match only when their normalized forms are equal; there is no fuzzy matching.
/// </summary>
public static class TitleNormalizer
{
    // Letters NFKD does not decompose into base + combining mark.
    private static readonly Dictionary<char, string> Ligatures = new()
    {
        ['ø'] = "o", ['æ'] = "ae", ['œ'] = "oe", ['ß'] = "ss", ['ł'] = "l", ['đ'] = "d", ['ð'] = "d", ['þ'] = "th",
    };

    public static string NormalizeAlbum(string title) => Base(title);

    /// <summary>Rules 1–2: NFKC, strip diacritics, case-fold.</summary>
    private static string Base(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormKD).ToLowerInvariant();
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(Ligatures.TryGetValue(ch, out var plain) ? plain : ch.ToString());
        }

        return sb.ToString().Normalize(NormalizationForm.FormKC);
    }
}
