# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
param([Parameter(Mandatory)][string] $Godot)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
Push-Location $repository
try {
    dotnet msbuild testbed-gdscript/host/gdscript.2dog.csproj -t:StageGDScript
    if ($LASTEXITCODE -ne 0) {
        throw 'GDScript staging failed.'
    }
    foreach ($template in 'web_dlink_nothreads_release.zip', 'web_dlink_release.zip', 'windows_release_x86_64.exe') {
        if (!(Test-Path -LiteralPath "artifacts/godot-templates/$template")) {
            throw 'Run scripts/Get-GodotExportTemplates.ps1 first.'
        }
    }
    # Godot 4.7.2 can crash on exit after registering an extension it found during the scan (debug_draw_3d does
    # too); the list is written before that. Once registered at startup, the exports below exit cleanly.
    & $Godot --headless --path testbed-gdscript --import *> artifacts/gdscript-import.log
    # A crash reports a negative (Windows) or 128+ exit code; anything else is a failed import.
    if ($LASTEXITCODE -gt 0 -and $LASTEXITCODE -lt 128) {
        throw "Import failed (exit $LASTEXITCODE; see artifacts/gdscript-import.log)."
    }
    if (!(Select-String -LiteralPath testbed-gdscript/.godot/extension_list.cfg -Pattern 'tweens_gd.gdextension' -Quiet)) {
        throw 'The import did not register the tweens_gd extension (see artifacts/gdscript-import.log).'
    }
    New-Item -ItemType Directory -Path artifacts/gdscript-windows, artifacts/gdscript-web, artifacts/gdscript-web-threads -Force | Out-Null
    foreach ($preset in 'Windows Tests', 'Web Tests', 'Web Threads Tests') {
        $log = 'artifacts/gdscript-export-' + $preset.Replace(' Tests', '').Replace(' ', '-').ToLowerInvariant() + '.log'
        & $Godot --headless --path testbed-gdscript --export-release $preset *> $log
        if ($LASTEXITCODE -ne 0) {
            $exitCode = $LASTEXITCODE
            Get-Content -LiteralPath $log -Tail 25
            throw "Export failed: $preset (exit $exitCode; see $log)"
        }
    }
    $testOptions = @{
        FilePath = Join-Path $repository 'artifacts/gdscript-windows/tests.exe'
        ArgumentList = '--headless'
        WindowStyle = 'Hidden'
        Wait = $true
        PassThru = $true
        RedirectStandardOutput = 'artifacts/gdscript-windows.log'
        RedirectStandardError = 'artifacts/gdscript-windows-errors.log'
    }
    $test = Start-Process @testOptions
    Get-Content -LiteralPath artifacts/gdscript-windows.log
    if ($test.ExitCode -ne 0) {
        Get-Content -LiteralPath artifacts/gdscript-windows-errors.log -Tail 25
        throw 'Windows export tests failed.'
    }
    if (!(Select-String -LiteralPath artifacts/gdscript-windows.log -Pattern 'GDScript: \d+ checks, 0 failures\.')) {
        throw 'Windows suite did not report completion.'
    }
    Write-Output 'All release exports built. Run node scripts/test-gdscript-web.mjs [gdscript-web-threads] to test Web/WASM.'
}
finally {
    Pop-Location
}
