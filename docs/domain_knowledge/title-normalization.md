# Title normalization

The single, deterministic transformation applied to album and track titles before any
comparison: matching a library album to a Release, matching tracks for the ownership check, and
merging the same Release across Sources. Two titles match only when their normalized forms are
equal. There is no fuzzy or similarity matching.

## Rules, in order

1. Unicode normalize to NFKC, then strip diacritics (`é` → `e`, `ø` → `o`).
2. Case-fold.
3. Replace `&` with `and`.
4. Remove all punctuation and symbols; keep letters, digits, and spaces.
5. Collapse runs of whitespace to one space; trim.

### Albums only

6. Remove one trailing edition qualifier when present. A qualifier is a final segment in
   parentheses or brackets, or after a spaced dash, containing any of: `deluxe`, `remaster`,
   `remastered`, `expanded`, `anniversary`, `explicit`, `bonus`, `edition`, `version`,
   `special`, `collector`, `limited`, `24-bit`, `hd`. Examples: `Album (Deluxe Edition)`,
   `Album [Explicit]`, `Album - 10th Anniversary Edition`, `Album (24-bit HD audio)`.

### Tracks only

7. Remove one trailing featured-artist segment when present: `(feat. …)`, `(ft. …)`,
   `[feat. …]`, or ` feat. …` / ` ft. …` to end of string.

## Boundaries

- Rule 6 removes at most one qualifier, from the end only. A qualifier in the middle of a title
  is part of the title.
- Rule 7 never removes the featured artist from the *artist* field; it applies to titles only.
- Leading articles (`The`, `A`) are kept. `The Album` and `Album` are different works.
- Numbers are kept as written; `Vol. 2` and `Volume 2` do not match (punctuation removal turns
  `Vol.` into `vol`, which still differs from `volume`).

## Examples

| Input | Normalized |
| --- | --- |
| `TANZNEID (24-bit HD audio)` | `tanzneid` |
| `Random Access Memories [Explicit]` | `random access memories` |
| `Get Lucky (feat. Pharrell Williams)` (track) | `get lucky` |
| `Café Bleu` | `cafe bleu` |
| `Rock & Roll` | `rock and roll` |
