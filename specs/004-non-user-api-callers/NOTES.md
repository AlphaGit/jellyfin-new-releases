# Seed: an API-key caller gets 400, not 401

Found by the real-server install of 0.1.0 on Jellyfin 12.1 — see
[`docs/real-server-install-0.1.0.md`](../../docs/real-server-install-0.1.0.md), finding 1.
`spec.md` for `003-jellyfin-12-compat` says a real-server finding "becomes its own specification".
This is the seed for that, not the specification itself. Run `/speckit-specify` to write one.

## The defect

`GET /Plugins/NewReleases/api/releases` and `.../artists`, called with a Jellyfin API key, return
**400** and log an error:

```
System.ArgumentException: Guid can't be empty (Parameter 'id')
   at Jellyfin.Server.Implementations.Users.UserManager.GetUserById(Guid id)
   at Jellyfin.Plugin.NewReleases.Api.ReleasesController.AccessOf(Guid userId)
```

An API-key caller has no user. Jellyfin represents that as `Guid.Empty`, not as a missing claim,
so `ReleasesController`'s guard does not fire and the empty GUID reaches `GetUserById`.

## Why the suite is green anyway

`ReleasesControllerTests::GetReleases_WithoutTheUserIdClaim_Is401` constructs a controller context
with **no claim at all**. The real host supplies a claim holding `Guid.Empty`. The double and the
host disagree, and the double is wrong. Any fix must change how
`Support/ControllerContextFactory.cs` models an unauthenticated caller, or the same class of bug
returns.

## Questions for the specification

- What should a non-user caller receive — 401, or 403? 401 matches the existing test's intent.
- Does the same hole exist on the decision endpoints (`ignore`, `have-it`, `restore`) and on
  `api/status`? `api/status` answered 200 with an API key, so it does not read the user; the
  decision endpoints were not exercised in this pass and must be checked.
- Should `AdminController` behave the same way? It requires elevation and answered 200 with the
  API key, which is correct for an admin-scoped key but should be stated deliberately.
