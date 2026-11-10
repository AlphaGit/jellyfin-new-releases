# Quickstart: validate Want it list

## Suite gate

```bash
export PATH=/opt/homebrew/opt/dotnet/bin:$PATH   # the plugin needs SDK 10
dotnet build --configuration Release             # zero warnings
dotnet test                                      # hermetic, no network
node --test tests/web                            # page tests
```

Expected: all green. New tests cover the `004` migration, Clear Archive, the `wanted` view, the
`WantIt` action, the `400`, library access, and the buttons per view.

## Scenario checks (covered by the acceptance tests)

1. Mark a listed Release Want it. It leaves the List and shows in the Want-it list (US1).
2. In the Want-it list, mark one Have it and one Ignore. Both show in the Archive (US2).
3. Restore a Want-it Release. It returns to the List (US2).
4. Mark an archived Release Want it. It leaves the Archive (FR-006b).
5. Purge release data, refresh. The Want-it list is the same (US3).
6. Clear Archive. The Want-it list is unchanged (US3).

## Real-server pass (the maintainer's own, after release)

Upgrade a server that holds Ignore and Have-it decisions. Confirm the Archive is unchanged, then
walk steps 1 to 6 on the page. Defects found there become new specs.
