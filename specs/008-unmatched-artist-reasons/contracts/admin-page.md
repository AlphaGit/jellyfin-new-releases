# Page contract: the Unmatched artists table in `admin.html`

The page renders `unmatched[]` of the admin status response
([http-api.md](./http-api.md)). It parses no reason text (FR-013). All wording below lives in the
page (research R5).

## Reason column

One entry per `reasons[]` entry, `<displayName>:` followed by:

| Entry | Line content |
| --- | --- |
| `Matched` | `Matched` |
| `NotChecked` | `Not checked` |
| `Unmatched`, known `failedStep` | The trail: a bulleted list, one step per bullet as `<short name> <mark>` |
| `Unmatched`, `failedStep` null | The stored `reason` text |

Short names: `Id` → `ID`; the other step names show as they are.

| Step state | Mark | Words for a screen reader | Style |
| --- | --- | --- | --- |
| `Passed` | `✓` | passed | normal |
| `Failed` | `✗` | failed | normal |
| `NotReached` | `–` | not reached | dimmed |

The failed step's `values` follow its mark: `Score ✗ 70`, `Unique ✗ 100 vs 96`. The mark is
`aria-hidden`; the words are visually hidden text (research R6). A step does not wrap.

The table separates rows with a line and aligns every cell to the top. The artist name is
left-aligned. Artist takes 25% of the width, Reason only the width of its longest step, and How to
fix the rest (research R7).

## How to fix column

One paragraph per `Unmatched` entry with a known `failedStep`, in `reasons` order. `{…}` is filled
from the entry. When `candidates` is `null`, the parts marked *(detail)* are left out.

| Source, failed step | Fix |
| --- | --- |
| MusicBrainz `Search` | Set the MusicBrainz artist ID in Jellyfin's metadata editor or artist.nfo; the plugin uses it on the next refresh. |
| MusicBrainz `Score`, `Unique` | The same sentence, then *(detail)* `Candidates:` and each candidate as `{name} ({score})` and its `id` in a `<code>` that selects as a whole on click. |
| Deezer `Name`, candidates not empty | *(detail)* Deezer lists {names, comma-separated}. If one is this artist, its name on Deezer differs from the name in Jellyfin; Deezer matches by name only. |
| Deezer `Name`, candidates empty or `null` | Deezer may not list this artist. |
| Deezer `Album` | *(detail)* Checked {exactNameCount} Deezer artists with this name and {albumsCompared} of their albums. None has the same title as an album in the library, ignoring case, punctuation, accents and one trailing edition tag such as (Deluxe Edition). Only the first 100 albums of each artist are compared. — Without detail, the sentence starts at "No Deezer album has the same title …". |

No fix mentions a source that did not fail. No two paragraphs on a row are the same (FR-007).

The last line of the column is the tracking line (FR-009):

- `Tracked through {displayName of each Matched entry, joined by ", "}.` when any entry is
  `Matched`;
- `Not tracked.` otherwise.

## Unchanged

The artist name links to its Jellyfin page, as today. The empty-table line and the table's
`hidden` rule stay as they are. Every value is escaped through the page's `esc`.
