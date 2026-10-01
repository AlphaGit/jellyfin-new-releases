# Specification Quality Checklist: An upgrade leaves exactly one version of the plugin running

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-01
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

Revalidated after grilling (5 rounds, 9 questions) and after the replan. 16/16 → 16/16.

- The spec body names no language, framework or API. `NOTES.md` carries the raw evidence —
  exception type, file paths, log lines — deliberately, as the seed record rather than the
  specification.
- The host's grouping behaviour is no longer an inference. It is confirmed against the host's own
  source and stated as fact; the one remaining uncertainty — which code path persists which name —
  is recorded in Assumptions along with why the feature does not depend on it.
- The request that prompted this feature ("fix the deployment pipeline so the previous version is
  removed") is answered as an outcome, because the publishing workflow cannot reach a user's server.
  Grilling settled the mechanism: the plugin removes retired copies of itself at startup.
- The automatic cleanup specified in round 2 was removed in round 5, after reading the host's
  source showed it already deletes superseded same-name copies at discovery. The requirement it
  rested on — that the host never retires a stale copy — was true only across names. The spec now
  carries a one-time documented removal instead, and `plan.md` records the correction.
- One bounded constitution exception is recorded rather than hidden: the renaming release asks the
  operator to delete one directory. Constitution IV governs the finished product, not a pre-release
  transition.
