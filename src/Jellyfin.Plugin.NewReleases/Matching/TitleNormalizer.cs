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

    public static string NormalizeAlbum(string title) => Finish(Base(title));

    /// <summary>Rules 1–3: NFKC, strip diacritics, case-fold, <c>&amp;</c> → <c>and</c>.</summary>
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

        return sb.ToString().Normalize(NormalizationForm.FormKC).Replace("&", "and", StringComparison.Ordinal);
    }

    /// <summary>Rules 4–5: keep letters, digits and spaces; collapse whitespace; trim.</summary>
    private static string Finish(string text)
    {
        var sb = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingSpace && sb.Length > 0)
                {
                    sb.Append(' ');
                }

                pendingSpace = false;
                sb.Append(ch);
            }
            else if (char.IsWhiteSpace(ch))
            {
                pendingSpace = true;
            }
        }

        return sb.ToString();
    }
}
