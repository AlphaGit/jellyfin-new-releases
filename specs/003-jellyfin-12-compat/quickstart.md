# Quickstart: Run on Jellyfin 12

How to set the machine up, build, test, and prove the publishing chain — without a Jellyfin server
and without publishing anything.

## Prerequisites

The .NET 10 SDK. Homebrew already manages `dotnet@8` and `dotnet@9` here; `dotnet@10` is an alias of
the `dotnet` formula and installs beside them.

```bash
brew install dotnet          # dotnet@10, currently SDK 10.0.400
dotnet --list-sdks           # expect a 10.0.x entry
```

`~/.zshenv` puts `dotnet@9` first on `PATH`. Prefix a command to pick .NET 10 explicitly:

```bash
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet build
```

Also needed, both already present: Node 22+ for the page tests, and Python with JPRM only if you
want to exercise packaging locally (`pip install jprm==1.1.0`).

## Build and test

```bash
dotnet restore --force-evaluate      # regenerates both packages.lock.json for the new pins
dotnet build --configuration Release # must produce zero warnings; TreatWarningsAsErrors is on
dotnet test  --configuration Release
node --test "tests/web/*.test.js"
```

Expected: the whole suite green, with no network access and no Jellyfin server anywhere. That
combination is the feature's completion condition — `SC-001`, `SC-002`, `SC-005`.

A single test while working the red-green loop:

```bash
dotnet test --filter "FullyQualifiedName~PluginPagesRegistration"
```

## Prove the retarget landed

```bash
grep -n 'TargetFramework\|Jellyfin\.\|Microsoft.Data.Sqlite' \
  src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj
```

Expect `net10.0`, `Jellyfin.Controller` / `Jellyfin.Model` / `Jellyfin.Data` /
`Jellyfin.Database.Implementations` all at `12.0.0` with `ExcludeAssets=runtime`, and
`Microsoft.Data.Sqlite` at `10.0.11`. Pins and their publication dates: [research.md](./research.md) `R4`.

Confirm the plugin took on no new dependency for the page integration:

```bash
grep -rn 'PluginPages\|Newtonsoft' src/Jellyfin.Plugin.NewReleases/*.csproj   # expect no match
```

## Prove the page registration

Covered by tests, not by a running server. The contract lists the statements each one asserts:
[contracts/plugin-pages-registration.md](./contracts/plugin-pages-registration.md).

The two that matter most:

- With a stand-in Plugin Pages loaded, starting the hosted service calls `RegisterPage` once with
  the payload in [data-model.md](./data-model.md), and stopping it calls `RemovePage` once.
- With no Plugin Pages at all, starting and stopping both succeed silently apart from a single log
  line, and everything else in the plugin still works.

## Prove the publishing chain without publishing

Build a package exactly as the release workflow would, and add it to a throwaway copy of the
manifest:

```bash
mkdir -p /tmp/jf12-check && cp repo/manifest.json /tmp/jf12-check/

jprm plugin build . --version 0.0.1 --output /tmp/jf12-check
jprm repo add --url "https://example.invalid/repo" /tmp/jf12-check/manifest.json \
  /tmp/jf12-check/jellyfin-new-releases_0.0.1.zip

python3 -m json.tool /tmp/jf12-check/manifest.json
unzip -l /tmp/jf12-check/jellyfin-new-releases_0.0.1.zip
```

Check against [contracts/plugin-repository-manifest.md](./contracts/plugin-repository-manifest.md):

- the entry carries `targetAbi` `12.0.0.0`, a `sourceUrl`, a `checksum` and a `timestamp`;
- the `guid` is `b8a15db8-e368-42c4-9048-390faf0094db`;
- the zip holds the plugin DLL, the four SQLite assemblies, `runtimes/linux-x64/native/libe_sqlite3.so`
  and `meta.json`;
- `git diff --stat src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj` is empty —
  JPRM rewrites `<TargetFramework>` during a build and must leave it as it found it.

Delete `/tmp/jf12-check` afterwards. Nothing in this section touches `repo/`, and no tag is created:
tagging a release is out of scope for this feature.

## What is not verified here

Whether the plugin loads into a real Jellyfin 12 server, whether the configuration page renders in
its dashboard, and whether the user view appears in its menu. That pass is the maintainer's own, run
outside this feature; anything it finds becomes its own specification. The web-client contract those
pages depend on was checked against `jellyfin-web` at tag `v12.0` and is intact —
[research.md](./research.md) `R7`.
