# Phase 0 Research: Make the pages read what the server actually sends

**Feature**: `005-page-json-casing` | **Date**: 2026-09-20

Every open question in the Technical Context is resolved here. Nothing below is a preference; each
decision is either measured against the Jellyfin 12.0.0 packages in this repository's lock file, or
forced by a requirement in `spec.md`.

---

## R1 — How the plugin states the naming of its own responses

**Decision**: each JSON-returning controller carries
`[Produces(JsonDefaults.CamelCaseMediaType)]`, from `Jellyfin.Extensions.Json`.

**Measured**, by reading `Jellyfin.Extensions` 12.0.0 metadata directly:

```text
TYPE Jellyfin.Extensions.Json.JsonDefaults
  FIELD Public,Static,Literal PascalCaseMediaType = application/json; profile="PascalCase"
  FIELD Public,Static,Literal CamelCaseMediaType  = application/json; profile="CamelCase"
  PROP Options | CamelCaseOptions | PascalCaseOptions
```

The two media types are the host's own negotiation handles — the same pair `spec.md` records being
driven against the review server, where the default naming and the explicitly requested one returned
identical values under different names. `ProducesAttribute` sets `ObjectResult.ContentTypes`, so the
host selects the output formatter registered for that profile instead of the default one. The plugin
therefore states its naming rather than inheriting it, which is exactly `FR-003`.

**Dependency check (constitution VI)**: `Jellyfin.Extensions` 12.0.0 already resolves through
`Jellyfin.Controller` 12.0.0 — `packages.lock.json` line 45 carries it as a transitive of a pinned
direct reference. **No new direct dependency is added, so no 7-day publication check applies.**

**Alternatives considered**

| Alternative | Rejected because |
| --- | --- |
| `[JsonPropertyName]` on every DTO property | ~50 attributes, restates each name by hand, and gives nothing structural to check. It also states the naming property-by-property rather than endpoint-by-endpoint, so `FR-010` ("every endpoint that returns a body") has no subject. |
| `return new JsonResult(value, JsonDefaults.CamelCaseOptions)` per action | Bypasses content negotiation instead of using it, changes every action's return shape, and loses `ActionResult<T>`. |
| Set MVC's `JsonOptions` from the plugin | The plugin does not own the host's MVC pipeline; changing it would rename every other plugin's and the server's own responses. |
| Make the pages read either naming | `spec.md` clarification rules it out explicitly: the contract must be the plugin's statement, not the pages' tolerance. |

## R2 — Where the declaration goes, and how "every endpoint" is checked

**Decision**: the attribute goes on the **controller class**, and a reflection test enforces it
structurally.

`ReleasesController` and `AdminController` get it at class level, which covers every current and
future action on them, including `AdminController.RunNow`, whose 409 body is an anonymous object no
`ProducesResponseType` describes. `UserViewController` does not get it: its single action already
declares `[Produces("text/html")]` and returns a fragment, not JSON — this is the one
**deliberate exception** `FR-012` and `SC-006` allow for, and it is recorded in the convention
document rather than left implicit.

**The structural rule** (`FR-010`, `SC-004`), stated so a test can be written from it rather than
from a list of today's endpoints:

> For every public action method on every `ControllerBase` subclass in the plugin assembly, the
> effective set of produced content types — the method's `ProducesAttribute` if it has one,
> otherwise its controller's — MUST either contain `JsonDefaults.CamelCaseMediaType` or contain no
> `application/json` type at all.

An action that produces JSON without the profile fails. An action that produces `text/html` passes.
A new controller added with no attribute at all fails, which is the recurrence `FR-010` names.

**Alternative rejected**: asserting a curated list of route/attribute pairs. `spec.md`'s clarification
requires the check to be structural "rather than a curated list that ages".

## R3 — Route names and their single source

**Decision**: one `internal static class PluginRoutes` of `const string` members, referenced by every
`[Route]` and by the Plugin Pages registration payload.

`const string` is required — `[Route]` takes a compile-time constant — and is sufficient, because
C# constant concatenation composes the derived paths:

```csharp
public const string Base = "Plugins/NewReleases";
public const string Admin = Base + "/Admin";
public const string UserView = Base + "/UserView";
public const string UserViewAbsolute = "/" + UserView;   // FR-013: the server-absolute form derived, not rewritten
```

**The renamed surface** (`FR-016`: PascalCase segments, multi-word concatenated, no `api` segment):

| Was | Becomes |
| --- | --- |
| `GET  Plugins/NewReleases/api/releases` | `GET  Plugins/NewReleases/Releases` |
| `GET  Plugins/NewReleases/api/artists` | `GET  Plugins/NewReleases/Artists` |
| `GET  Plugins/NewReleases/api/status` | `GET  Plugins/NewReleases/Status` |
| `POST Plugins/NewReleases/api/releases/{id}/ignore` | `POST Plugins/NewReleases/Releases/{id}/Ignore` |
| `POST Plugins/NewReleases/api/releases/{id}/have-it` | `POST Plugins/NewReleases/Releases/{id}/HaveIt` |
| `POST Plugins/NewReleases/api/releases/{id}/restore` | `POST Plugins/NewReleases/Releases/{id}/Restore` |
| `GET  Plugins/NewReleases/api/admin/status` | `GET  Plugins/NewReleases/Admin/Status` |
| `POST Plugins/NewReleases/api/admin/run-now` | `POST Plugins/NewReleases/Admin/RunNow` |
| `POST Plugins/NewReleases/api/admin/purge` | `POST Plugins/NewReleases/Admin/Purge` |
| `POST Plugins/NewReleases/api/admin/clear-archive` | `POST Plugins/NewReleases/Admin/ClearArchive` |
| `GET  Plugins/NewReleases/UserView` | unchanged |

`user-view.html` builds three of these paths from a `data-action` attribute (`ignore`, `have-it`,
`restore`). Those attribute values become `Ignore`, `HaveIt`, `Restore`. This is a route change, not
a naming compensation, so it does not breach `FR-003`'s "the pages MUST NOT be changed".

ASP.NET route matching is case-insensitive, so the old lower-case URLs would still resolve. That is
incidental and is **not** a compatibility promise; nothing outside the plugin calls them.

## R4 — The prefix inside the two pages, and what `SC-007` can mean here

**Decision**: `PluginRoutes` is the single authoritative source. The two pages each keep one `API`
literal, and **one test holds both literals to `PluginRoutes`**, failing if they diverge.

**Why the pages cannot read the constant.** Both are static embedded resources served with no build
step — a constitution constraint ("Web UI ships as embedded resources… No build step, no framework").
`admin.html` is streamed by Jellyfin's own dashboard controller straight from
`Plugin.GetPages()[].EmbeddedResourcePath`; the plugin never touches those bytes and has no hook to
substitute a token into them. Only `user-view.html` passes through plugin code
(`UserViewController`), so a substitution would cover one page and not the other.

**Why not substitute the one page we can.** It would add a second mechanism to solve half the
problem while the guard test is still needed for `admin.html`. Constitution VI settles it: one
mechanism, the cheaper one.

**What this means for `SC-007`.** The criterion originally read "changing the route prefix requires
editing exactly one place", which this design does not meet: the prefix has one *authoritative*
declaration and two *derived* literals. Raised, and **`spec.md` was amended on 2026-09-20**:
`SC-007` and `FR-013` now require one authoritative source plus a test that fails when a derived
literal disagrees with it. Editing only `PluginRoutes` turns the suite red naming the pages, so no
page can keep calling a route the plugin no longer serves.

## R5 — The test that fails when the naming stops matching

**Decision**: a **shared JSON fixture per response**, asserted from both sides.

```text
tests/fixtures/pages/releases.json       <- GET Plugins/NewReleases/Releases
tests/fixtures/pages/artists.json        <- GET Plugins/NewReleases/Artists
tests/fixtures/pages/admin-status.json   <- GET Plugins/NewReleases/Admin/Status
tests/fixtures/pages/status.json         <- GET Plugins/NewReleases/Status
```

- **C# side** (`FR-006`, `US3-AS1`): serialize a fully populated DTO through the host's own
  `JsonDefaults.CamelCaseOptions` and assert the result's property names match the fixture's, at
  every level. Rename a DTO property, or change the naming the endpoints declare, and this fails.
- **Node side** (`FR-007`, `FR-008`, `US3-AS2`, `US3-AS3`): run the page's `render` / `renderStatus`
  against the same fixture in the sandbox and assert the rendered output carries the real values.
  A page reading a field the fixture does not carry renders a blank or a dash, and the assertion
  fails.

The fixture is what makes the two languages meet without a running host. It is committed, not
generated at test time: a fixture regenerated by the code under test asserts nothing.

`JsonDefaults.CamelCaseOptions` is the same options instance the host's camelCase output formatter is
built from, so this is the host's serializer, not a local re-implementation of its rules.

**Alternative rejected**: asserting the DTO shape in C# only. That is precisely what the 257 existing
tests already do, and it is why the defect reached a real server.

## R6 — The stand-in for the page's surroundings, and its ceiling

**Decision**: extend `tests/web/load-page.js` with a **string-capturing fake DOM** written in this
repository. No third-party library (`FR-017`).

The pages only ever *write* markup — `innerHTML`, `textContent`, `insertAdjacentHTML`, `hidden`,
`setAttribute`. None of the behaviour under test reads back parsed structure. So the fake stores
what was written as a string and the assertions match against that string. **No HTML parser is
needed, and none is written.**

What it must support, which is all of it: `document.getElementById`, `document.querySelector`,
`document.createElement`, `element.querySelector`, `element.querySelectorAll`, `element.dataset`,
`element.addEventListener` (recorded, never fired), `innerHTML`, `textContent`, `hidden`,
`insertAdjacentHTML('beforeend', …)`, `appendChild`, `setAttribute`.

**Recorded as not covered** (`FR-018`): no layout, no CSS, no event dispatch, no `closest`
traversal over real ancestors, no markup parsing — so a test asserting on `panel.innerHTML` proves
what the page *wrote*, never what a browser would *render* from it. Escaping, well-formedness and
accessibility of that markup stay outside this stand-in, and `esc` keeps its own tests.

Reaching `render` and `renderStatus` from a test requires both pages to expose them on
`NewReleasesInternals`. `tests/web/exposure.test.js` pins that set exactly and **will fail on
purpose**; updating it is part of the work, and each newly exposed function arrives with a behaviour
on `tdd/test-list.md`, which is the rule that file states.

## R7 — Which documents are amended, and which are history

**Decision**: amend the **contracts** only.

| Document | Action | Why |
| --- | --- | --- |
| `specs/001-track-new-releases/contracts/http-api.md` | Amend routes | `FR-015`; it is the API contract |
| `specs/002-report-data-age/contracts/http-api.md` | Amend routes | `FR-015`; it is the API contract |
| `specs/003-jellyfin-12-compat/contracts/plugin-pages-registration.md` | No change | `/Plugins/NewReleases/UserView` is unchanged |
| `001`/`002` `tasks.md`, `tdd/test-list.md`, `tdd/cycle-log.md`, `tdd/verification.md` | Leave | Records of work that was done at a time, not statements of what the plugin serves |
| `docs/real-server-install-0.1.0.md`, `specs/004-…/NOTES.md` | Leave | Observations of a running server on a date |

The line drawn: **a contract states what the plugin serves now; a task list and a log state what
someone did then.** `SC-008` is read against the first kind. A test enforces it by scanning
`specs/**/contracts/*.md` for any `Plugins/NewReleases/...` path and requiring each to be a route the
plugin actually registers.

That test is a predicate, so the TDD profile's standing rule applies: write the accepting and
rejecting cases as a `[Theory]` table from the requirement **before** writing the scan.

## R8 — Where the convention is recorded

**Decision**: `docs/http-surface.md`, one page, linked from `CLAUDE.md`'s conventions list.

`FR-011` asks for "a place a future author will find before adding an endpoint". `CLAUDE.md` is what
an agent loads at session start, so the link belongs there; the rules themselves belong in a document
that can hold the exceptions list `FR-012` and `SC-006` require, without `CLAUDE.md` growing a
section every feature.

Not `.specify/memory/constitution.md`: the constitution governs how the project is built, and one
plugin's route casing is not a principle. Not a `specs/005/` contract alone: this convention outlives
the feature.

## R9 — The field enumeration, and a correction to the stated count

**Measured**, by extracting every property access on a response object from both page scripts:

- `user-view.html` reads **20** distinct names.
- `admin.html` reads **23** distinct names.
- **6** are shared (`id`, `jellyfinId`, `name`, `releasesLastCheckedAt`, `source`, `sources`).
- **Union: 37 distinct names.**

`spec.md` stated 24. **The measured number is 37.** `FR-002` requires the set to be "enumerated from
the pages rather than assumed", which is what produced this, so the enumeration governs. Raised, and
**`spec.md` was amended on 2026-09-20**: `SC-003` now says 37, as do the clarification entry and the
edge case that carried the estimate. Every one of the 37 is covered; the discrepancy changed no
scope, only a count.

The full enumeration, with the DTO property each name must come from, is in
[`data-model.md`](./data-model.md).

Two findings fall out of the enumeration and are recorded, not acted on:

- `GET Plugins/NewReleases/Status` is read by **neither** page. It is kept — `001` specifies it and
  `FR-009` forbids removing server behaviour — and it still declares the naming under `FR-010`.
- Ten returned properties on the three read responses are read by no page: `total`, `serverToday`,
  `datePrecision`, `decidedAt`, `displayName`, `enabled`, `lastSuccessAt`, `matchedArtists`,
  `startedAt` and `editionsFetched`. Unread is not wrong; `FR-007` is one-directional.

## R10 — Verification on a real server

**Decision**: a running Jellyfin 12 server is part of this feature's definition of done, and it is
**JD's own pass**, not an automated step.

`spec.md`'s Assumptions state it directly: "a suite going green again is not sufficient evidence",
because a green suite is exactly what shipped this defect. `SC-001`, `SC-002` and `SC-003` all say
"against a real server". [`quickstart.md`](./quickstart.md) carries the steps.

Anything that pass finds becomes its own specification, as `003`'s did.
