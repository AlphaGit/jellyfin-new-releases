namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>
/// The MusicBrainz artist IDs Jellyfin stores for one library artist (spec 009 FR-005, research R4).
/// A joint artist carries several, separated by <c>;</c>.
/// </summary>
public static class MusicBrainzIds
{
    /// <summary>Split on <c>;</c>, trim, drop empties and placeholders, distinct, in order.</summary>
    public static IReadOnlyList<string> Parse(string? value) => (value ?? string.Empty)
        .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Where(id => !PlaceholderArtists.IsPlaceholder(SourceLimits.MusicBrainz, id))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
