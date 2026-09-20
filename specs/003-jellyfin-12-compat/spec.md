# Feature Specification: Run on Jellyfin 12

**Feature Branch**: `003-jellyfin-12-compat`

**Created**: 2026-09-10

**Status**: Draft

**Input**: User description: "Migrate to be compatible with jellyfin 12"

## Context

The plugin is built and packaged for Jellyfin 10.11.x. Jellyfin 12 is a breaking host release: it
moves the server to a newer runtime, rewrites the library database, and raises the plugin
compatibility marker from `10.11.0.0` to `12.0.0.0`. Plugins packaged for 10.11 do not load on a
Jellyfin 12 server, and the Jellyfin upgrade guidance tells operators to remove third-party
plugins before upgrading. Jellyfin 12.0.0 and its plugin libraries were published on 2026-09-08.
The runtime the plugin targets today leaves support on 2026-11-10, so the move also puts the plugin
back on a supported runtime.

Everything the plugin does — the daily refresh, the sources, the ownership decision, the New
Releases page, the Archive, the admin page — stays the same. This feature changes only what the
plugin runs on, and how it is packaged, published, tested and described, so that an operator on
Jellyfin 12 can obtain the plugin that features `001-track-new-releases` and `002-report-data-age`
already specified.

## Clarifications

### Session 2026-09-10

- Q: Does the plugin keep supporting Jellyfin 10.11.x alongside Jellyfin 12, or move to Jellyfin 12 only? → A: Jellyfin 12 only. The plugin has never been released publicly, so dropping 10.11.x strands no operator. One package and one supported server version keep the build, the tests and the catalogue entry single-track; a second package would tax every future release for users who do not exist.

### Session 2026-09-12

- Q: May the development machine gain the toolchain Jellyfin 12 needs? → A: Yes. The newer toolchain is installed alongside the current one through the same package manager already used here, so both remain available and the red-green loop can run locally instead of through continuous integration.
- Q: Keep writing Plugin Pages' configuration file, or move to its new registration interface? → A: Move to the new interface. The plugin registers its page through the integration's supported registration call at startup and withdraws it on shutdown, instead of editing another plugin's configuration file.
- Q: Does that choice generalise? → A: Yes. Where Jellyfin 12 or an integration offers a newer supported way for a plugin to interact with it, the plugin adopts that way rather than the older one it still tolerates. Research found this applies only to the Plugin Pages registration: every other host surface the plugin uses is the current, non-deprecated one in Jellyfin 12.
- Q: User Story 2 promises a catalogue that offers only a compatible build, but the project publishes no plugin repository — narrow the story to what the package declares, or build the repository in this feature? → A: Build it. The feature adds a published plugin repository manifest at a stable public URL, and the release process keeps it current, so the catalogue promise is real and testable rather than aspirational.
- Q: What counts as verified for this feature — a green automated suite against the Jellyfin 12 libraries, or that plus a manual pass on a real Jellyfin 12 server? → A: The green automated suite alone. Verification on a real Jellyfin 12 server happens outside this feature, and anything it finds comes back as its own specification rather than widening this one.
- Q: The Jellyfin 12.0.0 libraries are newer than the 7-day supply-chain window — wait for them to age, or override the rule now? → A: Override now, for the Jellyfin 12.0.0 packages only. The override does not extend to any other dependency; every other pin still takes the newest version that is at least 7 days old.
- Q: User Story 1's acceptance scenarios all assert what only a running server shows, which this feature no longer verifies — how should they read? → A: Rewrite them as what the suite can assert against the Jellyfin 12 libraries: the entry point constructs, every registered service resolves, the refresh and the API behave as specified, and the configuration round-trips. The operator-facing outcome stays as the story's intent.
- Q: Does User Story 3, data preservation across the server upgrade, survive? → A: No. The plugin has never been released or installed, so there is no prior state to preserve. Jellyfin 12 is the only supported server version and migration from an earlier one is not a requirement. The story, its success criteria and the stored-data edge case are removed.
- Q: Does this feature cut the first public release, given the repository would otherwise list nothing? → A: No. The feature builds and proves the publishing chain — package, upload, repository update — but tagging a public version stays the maintainer's act, after the real-server pass. The repository legitimately lists nothing until then.
- Q: How should User Story 2 read, given no running server and no published version are in scope? → A: Rewrite it around the artefact the release chain produces. The project's GitHub Pages site hosts whatever the catalogue needs — the repository document and the packages it points at — published by the workflow, so every scenario is checkable without a server.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The plugin works on a Jellyfin 12 server (Priority: P1)

An operator runs Jellyfin 12. They install the plugin from the catalogue and restart the server.
The plugin appears in the plugin list as active, its configuration page opens, the refresh task
is listed and runs, and the New Releases page shows releases. Nothing about the plugin behaves
differently from how it behaves on the older server.

**Why this priority**: Without this the plugin does not exist for anyone on Jellyfin 12. Every
other story in this feature is worthless if the plugin does not load.

**Independent Test**: Build the plugin against the Jellyfin 12 libraries and run the suite against
them: construct the plugin entry point, resolve every service the plugin registers, drive the
refresh and the API, and round-trip the configuration.

**Acceptance Scenarios**:

1. **Given** the plugin built against the Jellyfin 12 libraries, **When** the plugin entry point is
   constructed with the host's application paths and serializer, **Then** it constructs, reports its
   fixed identity, and offers its configuration page.
2. **Given** the plugin's service registration is run against a container holding the Jellyfin 12
   host services, **When** each service the plugin registers is resolved, **Then** every one
   resolves, including the scheduled task and both release sources.
3. **Given** a music library of artists, albums and tracks presented through the Jellyfin 12 library
   reader, **When** the refresh runs, **Then** it produces the same Missing, Incomplete and Upcoming
   results the specification for `001-track-new-releases` defines.
4. **Given** an authenticated request handled against the Jellyfin 12 libraries, **When** the API is
   called for the list, for the Archive and for a decision, **Then** it answers as specified and
   per-user library access is still enforced.
5. **Given** a configuration with every field set, **When** it round-trips through the host's
   serializer, **Then** every value returns unchanged and no collection gains duplicate entries.
6. **Given** the page integration is present, **When** the plugin starts and later shuts down,
   **Then** it registers its page through the integration's registration interface and withdraws it
   on shutdown; **and given** the integration is absent, **Then** the plugin still starts and
   everything not depending on it still works.

---

### User Story 2 - Operators are offered only a build that fits their server (Priority: P2)

An operator adds the project's plugin repository to their Jellyfin server and browses the
catalogue. They are offered the plugin only when their server can actually run it. An operator
whose server is too old is not offered a build that would fail to load, and the documented
requirement tells them which Jellyfin version they need.

**Why this priority**: A plugin that installs and then refuses to load is worse than one that is
absent: it costs the operator a restart, a log hunt, and trust. The catalogue is the only place
that can prevent it.

**Independent Test**: Run the release chain for a tagged version and inspect what it publishes: the
repository document at the project's public site, the package it points at, and the server version
that both declare.

**Acceptance Scenarios**:

1. **Given** a tagged version, **When** the release runs, **Then** the project's public site serves
   a plugin repository document listing that version, and serves the package that document points
   at, with no manual editing step.
2. **Given** that repository document, **When** it is read, **Then** every listed version carries a
   download location that resolves, a checksum matching the package, and Jellyfin 12 as the server
   version it supports.
3. **Given** the built package, **When** its own compatibility declaration is read, **Then** it
   names Jellyfin 12, matching its entry in the repository document.
4. **Given** the project's documentation, **When** an operator looks for the requirement, **Then**
   it states the supported Jellyfin version and the repository address to add to their server.

---

### Edge Cases

- **The optional integration is absent or too old for Jellyfin 12.** The New Releases
  page is reached through the third-party Plugin Pages integration, which does publish a build for
  Jellyfin 12 and still accepts the registration the plugin already performs. An operator may still
  run Jellyfin 12 without it, or with a version that predates Jellyfin 12 support. In every such
  case the configuration page, the API and the refresh task MUST still work, the plugin MUST NOT
  fail to load, and the absence is logged at most once.
- **An operator sideloads the package onto a server older than Jellyfin 12.** The plugin does not
  load. The documented requirement must make the cause obvious; the plugin is not required to work.
- **A later point release of Jellyfin 12.** The plugin keeps loading and working across patch
  releases of Jellyfin 12, without a new package per patch.
- **The library database rewrite in Jellyfin 12.** The refresh reads the same artists, albums and
  track titles it reads today; a library that produces a given result under the current libraries
  produces the same result under the Jellyfin 12 libraries.
- **A library item Jellyfin 12 models differently.** The refresh skips what it cannot read and
  reports it as an unmatched artist rather than failing the whole run.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The plugin MUST load and activate on a Jellyfin 12 server, registering its
  configuration page, its API and its scheduled task. Within this feature that is evidenced against
  the Jellyfin 12 libraries — the entry point constructs, every registered service resolves, and the
  API and the scheduled task answer; confirmation on a running server happens outside it.
- **FR-002**: Every behaviour specified in `001-track-new-releases` and `002-report-data-age` MUST
  hold on Jellyfin 12, unchanged. This feature MUST NOT add, remove or alter user-visible
  behaviour.
- **FR-003**: The refresh MUST read the same library artists, albums and track titles through the
  Jellyfin 12 library reader that it reads today, so that ownership results are identical for an
  identical library.
- **FR-004**: The plugin MUST NOT be required to migrate data or configuration from an earlier
  Jellyfin version. It has never been released or installed, Jellyfin 12 is the only supported
  server version, and there is no prior installed state to preserve.
- **FR-005**: The plugin's published package MUST declare Jellyfin 12 as the server version it
  supports, so the plugin catalogue offers it to servers that can run it and withholds it from
  servers that cannot.
- **FR-006**: The plugin's user-facing documentation MUST state the supported Jellyfin version, the
  minimum version of the optional page integration that the plugin's registration needs, and how an
  operator adds the plugin repository to their server.
- **FR-007**: The plugin's identity MUST NOT change. Jellyfin keys an install by that identifier,
  and it is fixed for the life of the plugin.
- **FR-008**: Optional third-party integrations MUST remain optional. When an integration is
  unavailable on Jellyfin 12, the plugin still loads and every part of it that does not depend on
  that integration still works.
- **FR-009**: The published package MUST include every supporting file the plugin needs to run on
  Jellyfin 12, so that a catalogue install requires no manual file step.
- **FR-010**: The automated test suite MUST run against the Jellyfin 12 libraries and MUST stay
  hermetic — no network and no Jellyfin server required.
- **FR-011**: The project's automated build MUST verify the plugin against the Jellyfin 12
  libraries on every push, and MUST fail when the plugin no longer builds or its tests no longer
  pass there.
- **FR-012**: The project MUST support Jellyfin 12 only. Jellyfin 10.11.x is dropped: one published
  package, declaring Jellyfin 12 as its supported server version. The project MUST NOT publish or
  maintain a second package for an older server version.
- **FR-013**: The project MUST publish a plugin repository at a stable public URL that a Jellyfin
  server can be pointed at, served from the project's own hosting alongside the packages it points
  at. It MUST list every published version of the plugin with its download location, its checksum
  and the server version it supports.
- **FR-014**: Publishing a version MUST update the published repository and publish its package as
  part of the automated release, without a manual editing step, so the repository can never fall
  behind the versions that exist. Until the first version is tagged the repository correctly lists
  nothing; this feature delivers the publishing chain, not a published version.
- **FR-015**: The plugin MUST register its user-facing page through the integration's own supported
  registration interface, and MUST withdraw that registration when it shuts down. It MUST NOT reach
  into another plugin's stored configuration to do so.
- **FR-016**: Where Jellyfin 12, or an integration the plugin uses, offers a newer supported way for
  a plugin to interact with it, the plugin MUST use that way rather than an older one still
  tolerated for compatibility.

### Key Entities

- **Supported server version**: the Jellyfin release the published package declares itself
  compatible with. The plugin catalogue uses it to decide whether to offer the plugin.
- **Published package**: the artefact an operator installs, carrying the plugin, its supporting
  files, its version and its supported server version.
- **Plugin repository**: the document the project publishes and an operator points their server at.
  It lists the published packages and the server version each one supports.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every host surface the plugin uses resolves against the Jellyfin 12 libraries: the
  build produces no unresolved reference, and the automated suite exercises the plugin entry point,
  the API and the scheduled task against those libraries.
- **SC-002**: 100% of the acceptance scenarios specified for `001-track-new-releases` and
  `002-report-data-age` still pass against the Jellyfin 12 libraries.
- **SC-003**: A given music library produces, under the Jellyfin 12 libraries, the same set of
  Missing, Incomplete and Upcoming releases that the specifications for `001-track-new-releases`
  and `002-report-data-age` define.
- **SC-004**: Every version the published repository lists declares Jellyfin 12, and the package it
  points at declares the same, so no server can be offered a build it cannot load.
- **SC-005**: The full automated suite passes on a machine with no network and no Jellyfin server,
  with zero build warnings.
- **SC-006**: The supported Jellyfin version is discoverable from the project's documentation and
  from the catalogue entry, without reading source code.

## Assumptions

- The host surfaces the plugin already uses are the current, supported ones in Jellyfin 12. Every
  one was checked against the published Jellyfin 12 libraries: the plugin base class, the
  configuration page hook, the service registration hook, the library reader and its query, the
  scheduled task, the endpoint authorization policies, the plugin data paths, and the user entity
  with its library-permission settings are all present, unchanged in signature, and none is marked
  deprecated. The plugin interfaces Jellyfin 12 names as broken — its search engine, an
  authentication member, parts of the item repository, some user-manager members, and the relocation
  of alternate versions and playlist contents — are not used by this plugin.
- The one place the plugin uses an older interaction that a newer supported one replaces is its
  registration with the page integration. That is in scope for this feature.
- The plugin has never been released or installed anywhere. No operator holds plugin data, so this
  feature has nothing to migrate forward and no compatibility to keep with an earlier build.
- Jellyfin 12 does not change the meaning of an artist, an album, or a track title for the
  purposes of the ownership decision. Only how they are read may change.
- The Jellyfin 12.0.0 libraries are adopted under an explicit, recorded exception to the project's
  7-day dependency-age rule, because no older stable Jellyfin 12 release exists. The exception
  covers those packages only. Every other dependency this feature touches still takes the newest
  version that is at least 7 days old.
- The sources the plugin reads (MusicBrainz, Deezer) are unaffected by the server upgrade, so no
  source, rate limit, budget or fixture changes are in scope.
- The New Releases page and the configuration page keep working unchanged. Both are plain web pages
  with no build step, and every host function they call is still present in the Jellyfin 12 web
  client: the dashboard helpers for loading, alerts, confirmation and configuration-update results,
  and the client methods for requests, server identity and plugin configuration. The page-injection
  change in Jellyfin 12 is handled by the Plugin Pages integration, not by this plugin.
- The maintainer's development machine gains the toolchain Jellyfin 12 needs, installed beside the
  current one so both stay available. A running Jellyfin 12 server is not needed to complete this
  feature.
- The user-facing feature set is frozen for this feature. Capabilities newly offered by Jellyfin 12
  are out of scope and belong in their own specification.

## Out of Scope

- Any new user-visible capability, including capabilities Jellyfin 12 newly makes possible.
- Changes to the release sources, the ownership rules, the release types, or the page layouts.
- Support for Jellyfin 10.11.x or any older server version. Support for it ends with this
  feature; no package is published for it and no build verifies it.
- Migrating the operator's Jellyfin server itself. The operator upgrades their server; this
  feature only makes the plugin work once they have.
- Tagging and publishing the first public release. This feature builds and proves the chain that
  publishes one; deciding to run it is the maintainer's act.
- Verification on a running Jellyfin 12 server. That pass happens outside this feature. Anything it
  finds — the plugin failing to load, the configuration page not rendering, the user view not
  injecting into the rewritten web client — becomes its own specification. This feature is complete
  when the automated suite is green against the Jellyfin 12 libraries.
