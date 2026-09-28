# CI targets

GitHub Actions owns build ordering and timeouts. These MSBuild entry points also
work locally with the repository's .NET SDK:

```powershell
dotnet msbuild build/Coverage.proj
dotnet msbuild build/Smoke.proj -p:Version=0.1.0-pre
```

Pack the addon ZIP and NuGet packages into `artifacts/packages/` before running
`Smoke.proj` (see [RELEASING.md](../RELEASING.md)). Smoke tests always extract fresh
consumers from those artifacts. The plain Godot SDK fixture checks source-install
compilation; its separate 2dog host imports through `TwoDogImportGodotProject` and
runs both languages. The NuGet fixture restores the exact local version into an
isolated cache. Fixture props/targets block accidental repository settings.

Coverage defaults to Release with 99% line and 95% branch minimums for library
sources. `Configuration`, `MinimumLine`, `MinimumBranch`, `Rendering`, and `NoBuild`
can be supplied with `-p:`. `NoBuild=true` requires each selected suite to have
already been built. Each invocation writes a new run directory, so earlier
reports cannot improve the current result. To recheck a run, use
`-t:Report -p:RunDirectory=<absolute-run-directory>`.

The remaining release publishing scripts retain their failure-handling checks.
Addon generation/packing scripts retain the deterministic archive and generated
source checks. The optional standard-Godot export scripts and browser harness
exercise non-.NET Windows/Web exports locally; CI uses only NuGet-delivered 2dog.
