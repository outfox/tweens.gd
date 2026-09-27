# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
# Windows engines used by CI to verify the actual addon ZIP in standard Godot.
param([string] $Version = '4.7.2')
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a stable Godot version such as 4.7.2.' }
$repository = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $repository "artifacts/godot-engines/$Version"
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$base = "https://github.com/godotengine/godot-builds/releases/download/$Version-stable"
$sums = (Invoke-WebRequest -Uri "$base/SHA512-SUMS.txt").Content
if ($sums -is [byte[]]) { $sums = [Text.Encoding]::UTF8.GetString($sums) }
$engines = @{}
foreach ($variant in @(
    @{ Key = 'Godot'; Name = "Godot_v$Version-stable_win64"; Suffix = '.exe.zip' },
    @{ Key = 'GodotDotNet'; Name = "Godot_v$Version-stable_mono_win64"; Suffix = '.zip' }
)) {
    $asset = "$($variant.Name)$($variant.Suffix)"
    $line = @($sums -split "`n" | Where-Object { $_.Trim().EndsWith($asset) })
    if ($line.Count -ne 1) { throw "Official engine checksum not found: $asset" }
    $expected = ($line[0].Trim() -split '\s+')[0]
    $archive = Join-Path $destination $asset
    if (!(Test-Path -LiteralPath $archive)) { Invoke-WebRequest -Uri "$base/$asset" -OutFile $archive }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $expected) {
        throw "Godot engine checksum mismatch: $asset"
    }
    $extracted = Join-Path $destination $variant.Key
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $extracted, $true)
    $executables = @(Get-ChildItem -LiteralPath $extracted -Recurse -File -Filter "$($variant.Name)_console.exe")
    if ($executables.Count -ne 1) { throw "Expected one console engine in $asset" }
    $engines[$variant.Key] = $executables[0].FullName
}
[pscustomobject]$engines
