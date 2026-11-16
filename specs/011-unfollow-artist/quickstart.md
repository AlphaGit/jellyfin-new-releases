# Quickstart: validate Unfollow artist

## Suite gate

```bash
export PATH=/opt/homebrew/opt/dotnet/bin:$PATH   # the plugin needs SDK 10
dotnet build --configuration Release             # zero warnings
dotnet test                                      # hermetic, no network
node --test tests/web                            # page tests
```

Expected: all green. New tests cover the `005` migration, the repository, the Refresh skip, the
List condition and the `artistUnfollowed` flag, the Artists actions and list, the admin count
and reset, and the page button, tab and admin line. Details: [research.md](./research.md) R9.

## Scenario checks (covered by the acceptance tests)

1. Unfollow an artist with several listed Releases. All leave the List at once, with no
   confirmation. The artist shows in the Unfollowed artists tab (US1).
2. Run a Refresh. The stubbed sources receive no request for the artist (SC-003).
3. Mark one Release of an artist Want it and another Ignore, then Unfollow the artist. Both stay
   in their tabs and both rows show "Follow" (US3, FR-013).
4. Follow the artist. Its undecided Releases show in the List at once. The Want-it and Archive
   rows are unchanged (US2, FR-007).
5. Restore the Ignored Release while the artist is unfollowed. It does not show in the List
   until Follow (US3 scenario 4).
6. Run Purge release data and Clear Archive. The artist stays unfollowed (FR-015).
7. Remove the artist from the library, refresh, add it back, refresh. It is unfollowed again
   (FR-014).
8. Admin: the library count includes the artist, "Unfollowed artists: N" counts it, and an
   Unmatched unfollowed artist is not in the Unmatched section. "Clear unfollowed artists"
   follows everyone (US4).
9. A user without access to the artist's library gets `403` on Unfollow and Follow and does
   not see the artist in the tab (FR-011).

## Real-server pass (the maintainer's own, after release)

Upgrade a server that holds Ignore, Have-it and Want-it decisions. Confirm all three lists are
unchanged. Then walk steps 1 to 8 on the page. Defects found there become new specs.
