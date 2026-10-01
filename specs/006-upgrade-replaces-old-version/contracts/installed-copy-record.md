# Contract: the record Jellyfin keeps for an installed copy

Jellyfin writes a `meta.json` into every installed plugin directory. The plugin **reads** it to
decide what to remove and **never writes** it — the host owns this file.

Verified against a real install on Jellyfin 12.1.

## Location

```text
<IApplicationPaths.PluginsPath>/<name>_<version>/meta.json
```

The directory name is derived from the record's `name`, so it changes when the displayed name
changes. Never match on it — see rule 2.

## Shape

camelCase. Only the first four matter here; the rest are read by the host.

| Field | Type | Used for |
| --- | --- | --- |
| `guid` | string | the only field this plugin matches on |
| `version` | string, four-part | compared against the running version |
| `name` | string | **not** matched on; it is the field that differs and caused the defect |
| `status` | string | `Active`, `Superseded`, `Disabled`, … — **not** matched on; a stale-name copy is never marked |

Example, from the server where the defect was found:

```json
{ "guid": "b8a15db8-e368-42c4-9048-390faf0094db", "name": "New Releases",
  "version": "0.1.0.0", "status": "Superseded" }
{ "guid": "b8a15db8-e368-42c4-9048-390faf0094db", "name": "Jellyfin New Releases",
  "version": "0.1.1.0", "status": "Active" }
```

Same GUID, two names. That is the whole defect in four lines.

## The removal rule

A directory is removed when **all** of these hold:

1. Its `meta.json` parses and its `guid` equals this plugin's frozen GUID.
2. It is not the directory the running assembly was loaded from.
3. Its `version` parses and is **lower** than the running version.

Anything else is left alone: an unreadable or absent record, another plugin's GUID, an unparseable
version, a copy at or above the running version.

**Why not match on the directory name**: a rename makes it miss, and a similarly named plugin makes
it delete another author's files.

**Why not require `status`**: a copy filed under a name the host no longer groups is never marked
superseded. Requiring it would remove nothing in exactly the case this feature exists for.

**Why "lower than running" rather than "not running"**: an operator may deliberately run an older
version with a newer one disabled. Removing anything newer would destroy their rollback target.

## Failure

A removal that fails — permissions, a locked file, a half-extracted directory — is reported once per
run and does not stop startup or affect anything else. A cleanup that cannot finish must never be
worse than one that never ran.

Each successful removal is recorded by directory name, at Information level, so an operator can see
afterwards what was deleted.
