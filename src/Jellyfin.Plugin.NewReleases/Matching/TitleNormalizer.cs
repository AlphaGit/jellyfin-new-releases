using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>
/// Deterministic title normalization from <c>docs/domain_knowledge/title-normalization.md</c>.
/// Two titles match only when their normalized forms are equal; there is no fuzzy matching.
/// </summary>
public static partial class TitleNormalizer
{
    // Letters NFKD does not decompose into base + combining mark.
    private static readonly Dictionary<char, string> Ligatures = new()
    {
        ['ø'] = "o", ['æ'] = "ae", ['œ'] = "oe", ['ß'] = "ss", ['ł'] = "l", ['đ'] = "d", ['ð'] = "d", ['þ'] = "th",
    };

    // Rule 6: one trailing "(…)", "[…]" or " - …" segment that names an edition.
    private const string QualifierWords =
        @"\b(deluxe|remaster(ed)?|expanded|anniversary|explicit|bonus|edition|version|special|collector|limited|24-bit|hd)\b";

    [GeneratedRegex(@"\s*(\(([^()]*)\)|\[([^\[\]]*)\]|\s-\s([^-]*))\s*$")]
    private static partial Regex TrailingSegment();

    [GeneratedRegex(QualifierWords)]
    private static partial Regex QualifierWord();

    public static string NormalizeAlbum(string title) => Finish(StripEditionQualifier(Base(title)));

    // Rule 7: one trailing "(feat. …)", "[ft. …]" or " feat. …" segment.
    [GeneratedRegex(@"\s*([(\[]\s*(feat|ft)\.?\s[^)\]]*[)\]]|\s(feat|ft)\.?\s.*)$")]
    private static partial Regex TrailingFeaturedArtist();

    public static string NormalizeTrack(string title) => Finish(TrailingFeaturedArtist().Replace(Base(title), string.Empty, 1));

    /// <summary>Artist names: rules 1–5 only (no qualifier or featured-artist removal).</summary>
    public static string NormalizeName(string name) => Finish(Base(name));

    private static string StripEditionQualifier(string text)
    {
        var m = TrailingSegment().Match(text);
        if (!m.Success)
        {
            return text;
        }

        var segment = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Value;
        return QualifierWord().IsMatch(segment) ? text[..m.Index] : text;
    }

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
