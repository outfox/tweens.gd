# Releasing tweens.gd

Release versions come from tags such as `v0.1.3-beta`. The tag supplies the version
for both the addon ZIP and NuGet packages; the project file retains its local
development version. Tags with a SemVer prerelease suffix produce GitHub prereleases.
Release titles match the tag exactly, such as `v0.1.3-beta`, without a project-name prefix.

## Prepare

1. Review changes since the previous release and write `.github/releases/<tag>.md`.
   Lead with user-visible behavior changes and include any steps needed to preserve
   previous behavior, documentation improvements, download names, and a comparison
   link. These files are the versioned changelog and the source of GitHub release notes.
2. Run `.github/scripts/Get-ReleaseVersion.ps1 -Tag <tag>` and
   `.github/scripts/Test-ReleasePublishing.ps1`. Check the relevant library,
   GDScript, and documentation suites; `.github/workflows/ci.yml` defines the full checks.
3. Commit the notes and release-related changes, and ensure CI passes for the
   intended release commit.

## Publish

Create and push the release tag on the checked commit. The Release workflow runs
the full CI pipeline, builds native libraries, versions and verifies both package
formats, and smoke-tests fresh consumers before publishing the release assets.
Release publication requires nonempty hand-written notes for the tag; it does not
fall back to GitHub-generated notes. Reruns upload assets to an existing release
without replacing its edited notes.

The tag-driven workflow publishes directly once all checks succeed. Prepare the
notes in the repository before tagging; GitHub's release-by-tag lookup does not
find unpublished drafts, so a separate draft is not part of this workflow.

After creating the GitHub release, the workflow publishes the C# package and its
symbols to NuGet.org using Trusted Publishing. Only the publishing job can request
a GitHub OIDC token; `NuGet/login` exchanges it for a short-lived NuGet API key
immediately before the push. Reruns skip package versions already published.

## NuGet Trusted Publishing setup

1. Set the GitHub repository secret `NUGET_USER` to the NuGet.org **username** of
   the user who created the policy, rather than an email address or API key.
2. On NuGet.org, create a Trusted Publishing policy with these values:

   | Field | Value |
   | --- | --- |
   | Repository owner | `outfox` |
   | Repository | `tweens.gd` |
   | Workflow file | `release.yml` |
   | Environment | Leave empty; the workflow uses no environment |
   | Package glob | `tweens.gd` |

   Select the policy owner that owns the package, and allow publishing new packages
   and package versions if this is the first NuGet release. A policy for another
   repository or workflow, such as fennecs's `NuGet.yaml`, does not match this one.

See [NuGet's Trusted Publishing documentation](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
for policy ownership and activation details. No long-lived NuGet API key is needed.

To publish packages from an existing GitHub release, run the **Release** workflow
manually on `main` with its `tag` input, for example `v0.1.4-beta`. This validates
the tag's version and downloads its exact `.nupkg` and `.snupkg` release assets;
it does not rebuild the source or change the release tag or notes. Both files
must be present before NuGet login. This also provides a retry path if NuGet
publication failed after the GitHub release was created.

## Local package checks

After building the native libraries for all platforms named in the addon manifest:

```powershell
./scripts/Pack-Addon.ps1 -Version 0.1.3-beta
dotnet pack csharp/tweens.gd.csproj -c Release -p:Version=0.1.3-beta -o artifacts/packages
dotnet msbuild build/Smoke.proj -p:Version=0.1.3-beta
```

`-AllowMissingNative` is for local development only; release archives must include
every native library named in the manifest. Use CI to build and check all platforms.
