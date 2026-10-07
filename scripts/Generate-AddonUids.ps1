# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
param([switch] $Check)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$addon = Join-Path $repository 'addons/tweens_gd'
# Godot writes a .uid beside each script and extension it imports; shipping them keeps installs and updates clean.
$sources = @(Get-ChildItem -LiteralPath $addon -File -Recurse | Where-Object Extension -In '.gd', '.cs', '.gdextension')
$missing = @($sources | Where-Object { !(Test-Path -LiteralPath "$($_.FullName).uid") })
$uids = @(Get-ChildItem -LiteralPath $addon -Filter '*.uid' -File -Recurse)
$orphans = @($uids | Where-Object { !(Test-Path -LiteralPath $_.FullName.Substring(0, $_.FullName.Length - 4)) })
$invalid = @($uids | Where-Object {
        $_.FullName -notin $orphans.FullName -and (Get-Content -LiteralPath $_.FullName -Raw) -cnotmatch '^uid://[a-y0-8]{1,13}\r?\n?$' })
if ($invalid.Count) {
    throw "Malformed UID files: $($invalid.Name -join ', ')"
}
$relative = { param($file) [IO.Path]::GetRelativePath($addon, $file).Replace('\', '/') }
if ($Check) {
    $problems = @($missing | ForEach-Object { "missing $(& $relative "$($_.FullName).uid")" }) +
    @($orphans | ForEach-Object { "orphaned $(& $relative $_.FullName)" })
    if ($problems.Count) {
        $shown = ($problems | Select-Object -First 5) -join ', '
        throw "$($problems.Count) addon UIDs are stale ($shown). Run ./scripts/Generate-AddonUids.ps1 and commit the output."
    }
    Write-Output "Verified $($sources.Count) addon UIDs."
    return
}
foreach ($orphan in $orphans) {
    Remove-Item -LiteralPath $orphan.FullName
}
# Derive each UID from its res:// path so regeneration is stable; Godot encodes 63-bit ids in base 34 (a-y, 0-8).
$alphabet = 'abcdefghijklmnopqrstuvwxy012345678'
foreach ($source in $missing) {
    $path = [Text.Encoding]::UTF8.GetBytes("res://addons/tweens_gd/$(& $relative $source.FullName)")
    $id = [BitConverter]::ToUInt64([Security.Cryptography.SHA256]::HashData($path), 0) -shr 1
    $text = ''
    do {
        $step = [Math]::DivRem($id, [uint64]34)
        $text = $alphabet[[int]$step.Item2] + $text
        $id = $step.Item1
    } while ($id)
    [IO.File]::WriteAllText("$($source.FullName).uid", "uid://$text`n")
}
Write-Output "Created $($missing.Count) and removed $($orphans.Count) addon UIDs."
