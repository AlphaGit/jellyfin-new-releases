# Release types

How each Source's typing maps onto the plugin's Release types, and how the admin's type
selection decides whether a Release is included and which type badge it shows.

## Our types

Album, EP, Single, Compilation, Live, Remix, Soundtrack. Default enabled: Album, EP.

## Mapping from sources

| Source value | Our type |
| --- | --- |
| MusicBrainz primary `Album` | Album |
| MusicBrainz primary `EP` | EP |
| MusicBrainz primary `Single` | Single |
| MusicBrainz secondary `Compilation` | Compilation |
| MusicBrainz secondary `Live` | Live |
| MusicBrainz secondary `Remix` | Remix |
| MusicBrainz secondary `Soundtrack` | Soundtrack |
| MusicBrainz primary `Broadcast`, `Other`; secondary `DJ-mix`, `Mixtape/Street`, `Demo`, `Spokenword`, `Interview`, `Audiobook`, `Audio drama`, `Field recording` | none — always excluded |
| Deezer `album` | Album |
| Deezer `ep` | EP |
| Deezer `single` | Single |
| Deezer `compile` | Compilation |

Deezer cannot express Live, Remix, or Soundtrack; a Deezer-only Release keeps the type Deezer
gives it. When MusicBrainz also lists the Release, the MusicBrainz typing is canonical.

## Inclusion rule

A Release is included only when all hold:

1. Its primary type maps to an enabled type.
2. Every secondary type it carries maps to an enabled type.
3. It has at least one Edition with status Official (MusicBrainz release status). Editions with
   status Promotion, Bootleg, or Pseudo-release are ignored everywhere: they neither qualify a
   Release nor supply a track list for the ownership check. Deezer editions are always Official.

A Release with any unmapped type, or with no Official edition, is excluded regardless of the
selection. "Bootleg" is therefore not a type and has no admin toggle.

## Displayed type

The first secondary type present in the precedence order **Live, Remix, Soundtrack,
Compilation**; if there is none, the primary type. Exactly one badge per Release.

## Examples

- Album + Live, default selection → excluded (Live not enabled). With Live enabled → included,
  badge "Live".
- Album + Compilation + Live, Live and Compilation enabled → included, badge "Live".
- EP, default selection → included, badge "EP".
- Deezer `compile` only, default selection → excluded.
