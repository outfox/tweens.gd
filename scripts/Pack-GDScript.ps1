# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
param([string] $Version, [string] $OutputDirectory = 'artifacts/packages')
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
if (!$Version) {
    $project = [xml](Get-Content -LiteralPath (Join-Path $repository 'csharp/tweens.gd.csproj') -Raw)
    $Version = [string]$project.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') { throw 'Use a semantic package version.' }
$output = [IO.Path]::GetFullPath($OutputDirectory, $repository)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$destination = Join-Path $output "tweens.gd-gdscript-$Version.zip"
$source = Join-Path $repository 'addons/tweens_gd'
$files = @(Get-ChildItem -LiteralPath $source -File | Sort-Object Name)
if (!$files.Count -or !(Test-Path -LiteralPath (Join-Path $source 'tweens.gd'))) { throw 'Addon source is missing.' }
$stream = [IO.File]::Open($destination, [IO.FileMode]::Create)
$zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        if ($file.Extension -notin '.gd', '.uid', '.md' -and $file.Name -ne 'LICENSE') { throw "Unexpected addon file: $($file.Name)" }
        $entry = $zip.CreateEntry("addons/tweens_gd/$($file.Name)", [IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
        $content = $entry.Open()
        try { $bytes = [IO.File]::ReadAllBytes($file.FullName); $content.Write($bytes, 0, $bytes.Length) }
        finally { $content.Dispose() }
    }
} finally { $zip.Dispose(); $stream.Dispose() }
$verify = [IO.Compression.ZipFile]::OpenRead($destination)
try {
    $names = @($verify.Entries | ForEach-Object FullName)
    foreach ($required in 'tweens.gd', 'catalog.gd', 'adapter.gd', 'shader_adapter.gd', 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'README.md', 'CATALOG.md') {
        if ("addons/tweens_gd/$required" -notin $names) { throw "Package lacks $required" }
    }
    if ($names.Count -ne $files.Count) { throw 'Package entry count mismatch.' }
} finally { $verify.Dispose() }
Write-Output "Packaged $($files.Count) addon files: $destination"
