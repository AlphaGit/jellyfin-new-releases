---
feature: 005-page-json-casing
loop: outside-in
profile: .specify/memory/tdd-profile.md
spec_criteria: 11
planned_at: af02934
updated_at: af02934
suite_baseline: green
---

# Test List: Make the pages read what the server actually sends

## The red this feature must start from

Measured against `Jellyfin.Extensions` 12.0.0 before any test was written, so the loop
knows what a right-reason failure looks like here:

```text
JsonDefaults.Options           -> {"Items":[1],"Total":1,"HasStoredReleases":true, ...}
JsonDefaults.CamelCaseOptions  -> {"items":[1],"total":1,"hasStoredReleases":true, ...}
JsonDefaults.PascalCaseOptions -> {"Items":[1],"Total":1,"HasStoredReleases":true, ...}
```

`JsonDefaults.Options` — the host default an endpoint receives when it declares nothing —
carries **no naming policy**, so it writes the exact property names: `Items`, not `items`.
That is the production defect, and it is reproducible in-process.

**So the naming behaviours below resolve the serializer options *from the endpoint's own
declaration*, never by reaching for `CamelCaseOptions` directly:**

```text
effective [Produces] content types of the action, else of its controller
  contains JsonDefaults.CamelCaseMediaType   -> JsonDefaults.CamelCaseOptions
  contains JsonDefaults.PascalCaseMediaType  -> JsonDefaults.PascalCaseOptions
  declares no application/json type at all   -> JsonDefaults.Options        (the host default)
```

A test written the other way — serializing with `CamelCaseOptions` unconditionally — is green
today and proves nothing. It would be the same mistake the 257 existing tests made: asserting
the DTO shape on the near side of the serializer.

## A note on the acceptance level available here

`.specify/memory/tdd-profile.md` records `acceptance: null` — there is no host-level runner, and
this defect lives **between** the controller and the browser, where no C# test can reach. The
strongest test this repository can build is a pair that meets at a committed fixture:

- the C# side proves the endpoint's declaration produces exactly the fixture's names;
- the node side proves the page reads exactly the fixture's names.

Together they close the loop **without a host**. That is weaker than an end-to-end test and the
list says so. `SC-001`, `SC-002`, `SC-003` and `SC-005` are closed by the real-server pass
(`tasks.md` T054), not by this suite.

## Outer loop: acceptance behaviors

One per acceptance scenario in `spec.md`.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| A1 | With releases stored, the view renders a row per item and not the "waiting for its first refresh" message | US1-AS1 | characterization | PENDING | |
| A2 | With no releases stored, the view renders the "waiting for its first refresh" message and no row | US1-AS2 | characterization | PENDING | |
| A3 | With a filter applied, the view lists only the narrowed set | US1-AS3 | example | PENDING | |
| A4 | A rendered row shows its artist, title, type, date, state, missing track titles, compared edition and source links, none blank | US1-AS4 | characterization | PENDING | |
| A5 | After a completed refresh, the administrator page shows the last refresh instant, the releases-last-checked instant, the next run, the artists processed and the releases found, none a dash | US2-AS1 | characterization | PENDING | |
| A6 | The administrator page shows each source's health, calls today, daily budget, cooldown and last error | US2-AS2 | characterization | PENDING | |
| A7 | The administrator page lists the artists no source matched, each with its reason and the hint | US2-AS3 | characterization | PENDING | |
| A8 | With data older than one refresh interval, both pages show `002`'s staleness wording | US2-AS4 | example | PENDING | |
| A9 | Renaming the server's responses the way Jellyfin 12 renamed them fails at least one test | US3-AS1 | example | PENDING | |
| A10 | Changing a page to read a field the server does not send fails at least one test | US3-AS2 | example | PENDING | |
| A11 | The decision between the list and the empty state is exercised against a response of the shape the server really produces | US3-AS3 | characterization | PENDING | |

`A3` is `example`, not characterization: the filters build a query string the server already
honours, so what this feature must prove is that the narrowed response still renders. `A9` and
`A10` are the two deliberate-mutant runs in `quickstart.md` pass 1, scenarios 2 and 3 — each
is a behaviour of the **suite**, evidenced by a recorded mutant, not by a test file of its own.

**Not closable here**: `A1`–`A8` are closed against a fixture, not against a running host. Each
is repeated against a real Jellyfin 12 server in `tasks.md` T054, which is where `SC-001`,
`SC-002`, `SC-003` and `SC-005` are actually met.

## Inner loop: unit behaviors

### `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U1 | The naming produced by this controller's declared media type writes `ListResponse` with the names `tests/fixtures/pages/releases.json` carries, at every nesting level | FR-003, FR-006, SC-004 | example | DONE | `Api/ResponseNamingTests.cs::ListResponse_AsTheReleasesEndpointDeclaresIt_CarriesTheNamesTheListPageReads` |
| U2 | The same declaration writes `ArtistsResponse` with the names `tests/fixtures/pages/artists.json` carries | FR-003, FR-006 | example | PENDING | |
| U3 | The same declaration writes `StatusResponse` with the names `tests/fixtures/pages/status.json` carries, though no page reads it | FR-010 | example | PENDING | |
| U4 | `GET Plugins/NewReleases/Releases` is the registered route for the list | FR-016 | example | PENDING | |
| U5 | `GET Plugins/NewReleases/Artists` is the registered route for the artist filter | FR-016 | example | PENDING | |
| U6 | `GET Plugins/NewReleases/Status` is the registered route for the small status | FR-016 | example | PENDING | |
| U7 | `POST Plugins/NewReleases/Releases/{id}/Ignore`, `/HaveIt` and `/Restore` are the registered decision routes | FR-016 | example | PENDING | |
| U8 | The controller's route prefix is `PluginRoutes.Base`, not a literal of its own | FR-013, SC-007 | example | PENDING | |

`U1`–`U3` are red today for the reason recorded at the top of this file: with no declaration the
resolved options are `JsonDefaults.Options` and the names come out PascalCase.

### `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs`

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U9 | The naming produced by this controller's declared media type writes `AdminStatusResponse` with the names `tests/fixtures/pages/admin-status.json` carries, including `sources[]`, `lastRun`, `unmatched[]` and `unmatched[].sources[]` | FR-003, FR-006 | example | PENDING | |
| U10 | `GET Plugins/NewReleases/Admin/Status` is the registered status route | FR-016 | example | PENDING | |
| U11 | `POST Plugins/NewReleases/Admin/RunNow`, `/Purge` and `/ClearArchive` are the registered action routes | FR-016 | example | PENDING | |
| U12 | The controller's route prefix is `PluginRoutes.Admin`, not a literal of its own | FR-013, SC-007 | example | PENDING | |

### `src/Jellyfin.Plugin.NewReleases/Api/UserViewController.cs`

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U13 | The controller's route is `PluginRoutes.UserView` and the served path stays `Plugins/NewReleases/UserView` | FR-013, FR-016 | example | PENDING | |
| U14 | The controller declares `text/html` and no JSON profile, and is the only endpoint the naming rule exempts | FR-012, SC-006 | example | PENDING | |

### The plugin's HTTP surface, assembly-wide

Behaviours of the surface as a whole rather than of one controller. `U15` and `U18` are
**predicates**: their accepting and rejecting cases are written as a `[Theory]` table from the
requirement **before** the predicate exists. The profile records why — three consecutive
remediations on this project each fixed one defect and introduced another, every time because
the fix was demonstrated with a single example chosen after the implementation was written.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U15 | An action whose effective produced types include an `application/json` type without the camelCase profile fails the rule | FR-010, SC-004 | example | PENDING | |
| U16 | An action whose effective produced types include no `application/json` type passes the rule | FR-010, FR-012 | example | PENDING | |
| U17 | An action that inherits the camelCase profile from its controller rather than declaring its own passes the rule | FR-010 | example | PENDING | |
| U18 | Every action on every `ControllerBase` in the plugin assembly satisfies the rule | FR-010, SC-004 | example | PENDING | |
| U19 | A route segment that is lower-case fails the casing rule | FR-016, SC-006 | example | PENDING | |
| U20 | A route segment named `api` fails the casing rule | FR-016, SC-006 | example | PENDING | |
| U21 | A PascalCase multi-word segment with no separator passes the casing rule | FR-016, SC-006 | example | PENDING | |
| U22 | Every route the plugin registers satisfies the casing rule | FR-016, SC-006 | example | PENDING | |
| U23 | Each embedded page's `API` literal begins with `PluginRoutes.Base`, and the failure names the page that has not followed | FR-013, SC-007 | example | PENDING | |
| U24 | Every request path literal either page sends resolves to a route the plugin registers | FR-013, FR-014 | example | PENDING | |
| U25 | A path written in a contract document that the plugin does not serve fails the document rule | FR-015, SC-008 | example | PENDING | |
| U26 | A path written in a contract document that the plugin does serve passes the document rule | FR-015, SC-008 | example | PENDING | |
| U27 | No `specs/**/contracts/*.md` names a route the plugin does not serve | FR-015, SC-008 | example | PENDING | |

### `src/Jellyfin.Plugin.NewReleases/Integration/PluginPagesRegistrationService.cs`

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U28 | The registered page entry's `Url` equals `PluginRoutes.UserViewAbsolute` | FR-013, SC-007 | example | PENDING | |

### `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`

`render` is the one function that consumes a server response and the one function with no test.
It is **correct as written** — it reads the names the fixture carries — so these capture what it
already does against a real response shape. They are characterization behaviours and they
terminate at `BASELINE`; the value is the guard, not a red.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U29 | `render` with stored releases writes a row per item into the panel | US1-AS1, FR-008 | characterization | PENDING | |
| U30 | `render` with stored releases writes no "waiting for its first refresh" message | US1-AS1, FR-001 | characterization | PENDING | |
| U31 | `render` with `hasStoredReleases: false` writes the "waiting for its first refresh" message | US1-AS2, FR-004 | characterization | PENDING | |
| U32 | `render` with `hasStoredReleases: false` writes no row | US1-AS2, FR-004 | characterization | PENDING | |
| U33 | `render` with stored releases but an empty `items` writes "Nothing missing for this selection." | US1-AS3, FR-004 | characterization | PENDING | |
| U34 | A rendered row carries the item's artist name, title, type, date and state | US1-AS4, SC-003 | characterization | PENDING | |
| U35 | A rendered `Incomplete` row carries its missing track titles and its compared edition | US1-AS4, SC-003 | characterization | PENDING | |
| U36 | A rendered row carries one link per entry in `sources` | US1-AS4, SC-003 | characterization | PENDING | |
| U37 | On the Archive tab a rendered row carries its archived badge and the kind of the decision | FR-002 | characterization | PENDING | |
| U38 | `render` with `releasesLastCheckedAt: null` writes no staleness sentence and does not hide the list | FR-005 | characterization | PENDING | |
| U39 | The page requests `Releases` and `Artists`, and posts `Releases/{id}/Ignore`, `/HaveIt`, `/Restore` | FR-016 | example | PENDING | |
| U40 | `render` is on `NewReleasesInternals` and `exposure.test.js` asserts the enlarged set | FR-017 | example | PENDING | |

`U39` is red today: the page requests `releases`, `artists` and posts `have-it`.

`U38` distinguishes the first edge case in `spec.md` — a field legitimately absent from a field
the page cannot read. `stalenessText(null, …)` is already pinned by
`tests/web/staleness.test.js::no instant yields no sentence`; what is new is that the value
reaches it from a response of the real shape.

### `src/Jellyfin.Plugin.NewReleases/Web/admin.html`

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U41 | `renderStatus` writes the last refresh instant and its outcome, not a dash | US2-AS1, SC-002 | characterization | PENDING | |
| U42 | `renderStatus` writes the releases-last-checked sentence, not a dash | US2-AS1, SC-002 | characterization | PENDING | |
| U43 | `renderStatus` writes the next run instant, or "Running now" while a refresh runs | US2-AS1, SC-002 | characterization | PENDING | |
| U44 | `renderStatus` writes the artists processed as a count of the library total, and the releases found | US2-AS1, SC-002 | characterization | PENDING | |
| U45 | `renderStatus` writes each source's health, calls today and daily budget | US2-AS2 | characterization | PENDING | |
| U46 | `renderStatus` writes a cooling-down source's `cooldownUntil` instant and its last error | US2-AS2 | characterization | PENDING | |
| U47 | `renderStatus` writes one unmatched row per artist, with its per-source reasons and the hint | US2-AS3 | characterization | PENDING | |
| U48 | `renderStatus` with no unmatched artists hides the table and shows the empty line | US2-AS3 | characterization | PENDING | |
| U49 | `renderStatus` with `releasesLastCheckedAt: null` writes a dash rather than a sentence | FR-005 | characterization | PENDING | |
| U50 | The page requests `Status` and posts `RunNow`, `Purge`, `ClearArchive` | FR-016 | example | PENDING | |
| U51 | `renderStatus` is on `NewReleasesInternals` and `exposure.test.js` asserts the enlarged set | FR-017 | example | PENDING | |

`U50` is red today: the page requests `status` and posts `run-now`, `purge`, `clear-archive`
under an `api/admin/` prefix.

### `tests/web/fake-dom.js`

Test infrastructure, listed because `FR-017` and `FR-018` make it a deliverable with stated
limits rather than a private helper.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U52 | An element's `innerHTML`, `textContent`, `hidden` and `setAttribute` writes are readable back as what was written | FR-017 | example | PENDING | |
| U53 | `insertAdjacentHTML('beforeend', …)` appends to `innerHTML` rather than replacing it | FR-017 | example | PENDING | |
| U54 | A selector the page does not use answers `null`, so a page reaching for an element the fake does not model fails loudly | FR-017 | example | PENDING | |
| U55 | Loading a page through the extended sandbox leaves the existing helper tests green | FR-017 | example | PENDING | |

## Invariants and edge cases still to place

- **`002`'s staleness wording on the user page, driven from a real response.** `A8` covers it at
  the acceptance level and `U38`/`U49` cover the null instant, but no behaviour yet pins the
  *sentence* appearing on `user-view.html` when the age exceeds the interval and the value came
  from a fixture rather than from a helper argument. Place under `Web/user-view.html` before
  `A8` can be called met.
- **A source whose `lastError` is present while its health is `Ok`.** `healthText` suppresses the
  error in that case; the rule is pinned by `page-helpers.test.js`, but not against a real
  response shape.
- **A release whose `date` is null.** `groupOf` returns `Undated` and the row prints "Undated".
  Pinned at the helper level; not yet through `render`.

## Out of scope

- **Event dispatch.** `tests/web/fake-dom.js` records listeners and never fires them, so tab
  switching, filter changes, form submission and the Ignore / Have it / Restore buttons are not
  exercised by any behaviour above. `spec.md` Out of Scope keeps `row`, `refreshStatus`, `read`,
  `fill` and `query` outside this feature.
- **Markup correctness.** The fake captures strings and parses nothing, so no behaviour above
  asserts well-formedness, escaping in context, layout, focus or the accessibility tree. `esc`
  keeps its own tests.
- **Server-side behaviour.** `FR-009` forbids any change to what data is returned, to ownership
  rules, to sources or to stored data, so nothing on this list re-tests them.
- **The `004` defect** — a caller with no user receiving the wrong status. Separate feature,
  `specs/004-non-user-api-callers/`.
- **A browser test framework.** `FR-017` forbids it and `002` recorded the absence as deliberate.

## Verification commands

Copied verbatim from `.specify/memory/tdd-profile.md`:

- Single test (dotnet): `dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`
- Full suite (dotnet): `dotnet test --configuration Release`
- One file (node): `node --test tests/web/{file}`
- Full suite (node): `node --test "tests/web/*.test.js"`
- Locale check (node): `LANG=de_DE.UTF-8 node --test "tests/web/*.test.js"`
- Coverage: none. `coverlet.collector` is not referenced; the audit falls back to trace checking.
- Mutation: none. Stryker.NET is not installed; the audit uses deliberate mutants, restored from
  a file copy and verified with `cmp -s`. **Never `git checkout --`.**

`--` before `RunConfiguration.TreatNoTestsAsError=true` is mandatory. Without it a filter that
matches nothing exits 0, which turns every red into a false green. The node stack has no safe
single-test invocation for the same reason, so `file` is the unit of work there.
