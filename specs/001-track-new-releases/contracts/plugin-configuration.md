# Contract: PluginConfiguration

Persisted by Jellyfin as XML through `XmlSerializer`. Scalar properties only (R9): a missing
element keeps the property initializer's default, so no seeding step exists and round-trips are
lossless. Guarded by `Configuration/PluginConfigurationTests` (serialize → deserialize → equal).

| Property | Type | Default | Spec |
| --- | --- | --- | --- |
| `MusicBrainzEnabled` | bool | `true` | FR-010 |
| `DeezerEnabled` | bool | `true` | FR-010 |
| `IncludeAlbums` | bool | `true` | FR-004 |
| `IncludeEps` | bool | `true` | FR-004 |
| `IncludeSingles` | bool | `false` | FR-004 |
| `IncludeCompilations` | bool | `false` | FR-004 |
| `IncludeLive` | bool | `false` | FR-004 |
| `IncludeRemixes` | bool | `false` | FR-004 |
| `IncludeSoundtracks` | bool | `false` | FR-004 |
| `ReleasedSince` | string | `""` | FR-003; `yyyy-MM-dd` or empty = unrestricted |
| `UserAgentContact` | string | `""` | FR-018; URL or e-mail appended to the `User-Agent` |

Not configurable (constants in `Sources/SourceLimits.cs`, R10): request rates, daily budgets,
failure threshold, cooldown. Not present in v1 (R12): credentials, terms acceptance.

Helper (pure, on the configuration or a static mapper): `EnabledReleaseTypes()` →
`ISet<ReleaseType>` and `ReleasedSinceDate()` → `DateOnly?` (invalid text → `null`, logged once).

Refresh interval is **not** here: it is edited in Jellyfin's Scheduled Tasks for the task
"Refresh new releases" (category "New Releases"), default daily at 03:00 server time.
