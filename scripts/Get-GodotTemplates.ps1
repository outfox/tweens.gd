# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
param([string] $Version = '4.7.2', [switch] $KeepArchive)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a stable Godot version such as 4.7.2.' }
$repository = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repository 'artifacts'
$destination = Join-Path $artifacts 'godot-templates'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$manifestPath = Join-Path $destination 'verified.json'
if (Test-Path -LiteralPath $manifestPath) {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
    $valid = $manifest.version -eq $Version
    foreach ($file in $manifest.files.Keys) {
        $item = Join-Path $destination $file
        $valid = $valid -and (Test-Path -LiteralPath $item) -and ((Get-FileHash -LiteralPath $item -Algorithm SHA256).Hash -eq $manifest.files[$file])
    }
    if ($valid -and $manifest.files.Count -eq 6) { Write-Output "Verified cached Godot $Version templates."; return }
}
$base = "https://github.com/godotengine/godot-builds/releases/download/$Version-stable"
$asset = "Godot_v$Version-stable_export_templates.tpz"
$archivePath = Join-Path $artifacts "godot-$Version-templates.tpz"
$sums = (Invoke-WebRequest -Uri "$base/SHA512-SUMS.txt").Content
if ($sums -is [byte[]]) { $sums = [Text.Encoding]::UTF8.GetString($sums) }
$line = @($sums -split "`n" | Where-Object { $_.Trim().EndsWith($asset) })
if ($line.Count -ne 1) { throw 'Official template checksum not found.' }
$expected = ($line[0].Trim() -split '\s+')[0]
if (!(Test-Path -LiteralPath $archivePath)) { Invoke-WebRequest -Uri "$base/$asset" -OutFile $archivePath }
if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA512).Hash -ne $expected) { throw 'Godot template checksum mismatch.' }
$archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
$hashes = @{}
try {
    foreach ($entry in $archive.Entries) {
        # The dlink Web templates can load GDExtension side modules.
        if ($entry.Name -notmatch '^(web_dlink_(nothreads_)?(debug|release)\.zip|windows_(debug|release)_x86_64\.exe)$') { continue }
        $file = Join-Path $destination $entry.Name
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $file, $true)
        $hashes[$entry.Name] = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
    }
} finally { $archive.Dispose() }
if ($hashes.Count -ne 6) { throw 'The official archive did not contain all required templates.' }
@{ version = $Version; archive_sha512 = $expected; files = $hashes } | ConvertTo-Json | Set-Content -LiteralPath $manifestPath
if (!$KeepArchive) { Remove-Item -LiteralPath $archivePath }
Write-Output "Verified and extracted Windows/Web templates for Godot $Version."
