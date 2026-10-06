# Releasing tweens.gd

Release versions come from tags such as `v0.1.3-beta`. The tag supplies the version
for both the addon ZIP and NuGet packages; the project file retains its local
development version. Tags with a SemVer prerelease suffix produce GitHub prereleases.

## Prepare

1. Review changes since the previous release and write `.github/releases/<tag>.md`.
   Lead with user-visible behavior changes and include any steps needed to preserve
   previous behavior, documentation improvements, download names, and a comparison
   link. These files are the versioned changelog and the source of GitHub release notes.
2. Run `.github/scripts/Get-ReleaseVersion.ps1 -Tag <tag>` and
   `.github/scripts/Test-ReleasePublishing.ps1`. Check the relevant library,
   GDScript, and documentation suites; `.github/workflows/ci.yml` defines the full checks.
3. Commit the notes and release-related changes, and ensure CI passes for the
   intended release commit. A GitHub draft release can use the same notes file.

## Publish

Create and push the release tag on the checked commit. The Release workflow runs
the full CI pipeline, builds native libraries, versions and verifies both package
formats, and smoke-tests fresh consumers before uploading the release assets.
Release publication requires nonempty hand-written notes for the tag; it does not
fall back to GitHub-generated notes. Reruns upload assets to an existing release
without replacing its edited notes.

If a draft release already exists, the workflow uploads its assets and leaves it
as a draft. Publish that draft after the Release workflow succeeds and its notes
and downloads have been reviewed.

## Local package checks

After building the native libraries for all platforms named in the addon manifest:

```powershell
./scripts/Pack-Addon.ps1 -Version 0.1.3-beta
dotnet pack csharp/tweens.gd.csproj -c Release -p:Version=0.1.3-beta -o artifacts/packages
dotnet msbuild build/Smoke.proj -p:Version=0.1.3-beta
```

`-AllowMissingNative` is for local development only; release archives must include
every native library named in the manifest. Use CI to build and check all platforms.
