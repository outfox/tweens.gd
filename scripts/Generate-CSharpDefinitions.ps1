# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
param([switch] $Check)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
# A fresh emission directory prevents removed definitions surviving an incremental build.
$emission = Join-Path $repository "artifacts/generated-csharp/$([Guid]::NewGuid())"
dotnet build (Join-Path $repository 'csharp/tweens.gd.csproj') -c Release -f net8.0 --no-incremental `
    -p:EmitCompilerGeneratedFiles=true "-p:CompilerGeneratedFilesOutputPath=$emission"
if ($LASTEXITCODE -ne 0) {
    throw 'C# definition generation failed.'
}
# Never ship Godot's generators: the consuming Godot SDK must generate its own glue.
$source = Join-Path $emission 'tweens.gd.Generators'
$files = @(Get-ChildItem -LiteralPath $source -Filter '*.g.cs' -File -Recurse | Sort-Object Name)
if (!$files.Count) {
    throw 'No structured definitions were emitted.'
}
$destination = Join-Path $repository 'addons/tweens_gd/csharp/Generated'
$expected = @($files | ForEach-Object Name)
$existing = @(Get-ChildItem -LiteralPath $destination -Filter '*.g.cs' -File -ErrorAction SilentlyContinue)
$stale = @($existing | Where-Object Name -NotIn $expected)
if (!$Check) {
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    # Only remove obsolete generator-owned files in the fixed addon output directory.
    foreach ($file in $stale) {
        Remove-Item -LiteralPath $file.FullName
    }
}
$differences = @($stale | ForEach-Object Name)
foreach ($file in $files) {
    $target = Join-Path $destination $file.Name
    $content = [IO.File]::ReadAllText($file.FullName).Replace("`r`n", "`n")
    if ($Check) {
        if (!(Test-Path -LiteralPath $target) -or [IO.File]::ReadAllText($target).Replace("`r`n", "`n") -cne $content) {
            $differences += $file.Name
        }
    }
    else {
        [IO.File]::WriteAllText($target, $content)
    }
}
if ($Check -and $differences.Count) {
    throw "C# addon definitions are stale ($($differences.Count) files). Run ./scripts/Generate-CSharpDefinitions.ps1 and commit the output."
}
if (!$Check) {
    & (Join-Path $PSScriptRoot 'Generate-AddonUids.ps1') | Out-Null
}
Write-Output "$(if ($Check) { 'Verified' } else { 'Generated' }) $($files.Count) C# addon definitions."
