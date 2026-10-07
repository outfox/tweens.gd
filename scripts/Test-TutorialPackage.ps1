# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
param(
    [Parameter(Mandatory)][string] $Project,
    [Parameter(Mandatory)][string] $TwoDogVersion
)
$ErrorActionPreference = 'Stop'
# Build before import: 2dog's editor raises the extracted project's target to net10.0.
# The subsequent --no-build run must exercise the original net8.0 assembly instead.
dotnet build $Project -c Release "-p:TwoDogVersion=$TwoDogVersion" -p:TwoDogAutoImport=false
if ($LASTEXITCODE -ne 0) {
    throw 'Tutorial verification build failed.'
}
$log = Join-Path (Split-Path -Parent $Project) 'import.log'
for ($attempt = 1; $attempt -le 3; $attempt++) {
    dotnet msbuild $Project -t:TwoDogImportGodotProject -p:Configuration=Release "-p:TwoDogVersion=$TwoDogVersion" -v:minimal *> $log
    if ($LASTEXITCODE -eq 0) {
        Get-Content -LiteralPath $log -Tail 8
        break
    }
    $failure = Get-Content -LiteralPath $log -Raw
    # Godot can crash during the initial GDExtension import. Retry only that native
    # crash, never compilation failures, script errors or failing tutorial tests.
    if ($attempt -eq 3 -or $failure -notmatch '2dog\.import\.dll.*exited with code (-1073741819|139)' -or $failure -match '(SCRIPT ERROR|Parse Error)') {
        Get-Content -LiteralPath $log -Tail 40
        throw 'Tutorial project import failed.'
    }
    Write-Warning "Godot crashed during initial import; retrying ($attempt/3)."
}
dotnet test $Project -c Release --no-build --no-restore "-p:TwoDogVersion=$TwoDogVersion"
if ($LASTEXITCODE -ne 0) {
    throw 'Tutorial archive tests failed.'
}
