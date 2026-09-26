# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
param([Parameter(Mandatory)][string] $Godot)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
Push-Location $repository
try {
    & ./testbed-gdscript/Stage.ps1
    foreach ($template in 'web_release.zip', 'windows_release_x86_64.exe') {
        if (!(Test-Path -LiteralPath "artifacts/godot-templates/$template")) { throw 'Run scripts/Get-GodotTemplates.ps1 first.' }
    }
    New-Item -ItemType Directory -Path artifacts/gdscript-windows, artifacts/gdscript-web -Force | Out-Null
    foreach ($preset in 'Windows Tests', 'Web Tests') {
        $log = if ($preset -eq 'Windows Tests') { 'artifacts/gdscript-export-windows.log' } else { 'artifacts/gdscript-export-web.log' }
        & $Godot --headless --path testbed-gdscript --export-release $preset *> $log
        if ($LASTEXITCODE -ne 0) {
            $exitCode = $LASTEXITCODE
            Get-Content -LiteralPath $log -Tail 25
            throw "Export failed: $preset (exit $exitCode; see $log)"
        }
    }
    $test = Start-Process -FilePath (Join-Path $repository 'artifacts/gdscript-windows/tests.exe') -ArgumentList '--headless' -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput artifacts/gdscript-windows.log -RedirectStandardError artifacts/gdscript-windows-errors.log
    Get-Content -LiteralPath artifacts/gdscript-windows.log
    if ($test.ExitCode -ne 0) { Get-Content -LiteralPath artifacts/gdscript-windows-errors.log -Tail 25; throw 'Windows export tests failed.' }
    if (!(Select-String -LiteralPath artifacts/gdscript-windows.log -Pattern 'GDScript: \d+ checks, 0 failures\.')) { throw 'Windows suite did not report completion.' }
    Write-Output 'Both release exports built. Run node scripts/test-gdscript-web.mjs to test Web/WASM.'
} finally { Pop-Location }
