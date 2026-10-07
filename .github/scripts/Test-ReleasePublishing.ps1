# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss

$ErrorActionPreference = 'Stop'

# Exercise the production script without network access or release mutations.
function Invoke-WebRequest {
    param($Uri, $Headers, [switch] $SkipHttpErrorCheck)
    $releaseTestState.Lookups++
    if (!$SkipHttpErrorCheck) { throw 'Lookup must inspect HTTP error statuses.' }
    if ($releaseTestState.NetworkFailure) { throw 'Simulated network failure' }
    [pscustomobject]@{ StatusCode = $releaseTestState.StatusCode }
}
function Get-ChildItem {
    param($Path, [switch] $File)
    [pscustomobject]@{ FullName = 'tweens.gd-0.1.0.zip' }
    [pscustomobject]@{ FullName = 'tweens.gd-tutorial.zip' }
    [pscustomobject]@{ FullName = 'tweens.gd.0.1.0.nupkg' }
    [pscustomobject]@{ FullName = 'tweens.gd.0.1.0.snupkg' }
}
function gh {
    $releaseTestState.Commands.Add(@($args))
    $global:LASTEXITCODE = $releaseTestState.ExitCode
}

$cases = @(
    @{ Status = 200; Command = 'upload' },
    @{ Status = 404; Command = 'create' },
    @{ Status = 404; Command = 'create'; Prerelease = $true },
    @{ Status = 401; Error = 'HTTP 401' },
    @{ Status = 403; Error = 'HTTP 403' },
    @{ Status = 429; Error = 'HTTP 429' },
    @{ Status = 500; Error = 'HTTP 500' },
    @{ Status = 503; Error = 'HTTP 503' },
    @{ NetworkFailure = $true; Error = 'Simulated network failure' },
    @{ Status = 200; Command = 'upload'; ExitCode = 1; Error = 'gh exit code 1' },
    @{ Status = 404; Command = 'create'; ExitCode = 1; Error = 'gh exit code 1' },
    @{ Notes = 'Missing'; Error = 'Release notes are missing' },
    @{ Notes = 'Empty'; Error = 'Release notes are empty' }
)
$savedExitCode = $global:LASTEXITCODE
$notesFile = [IO.Path]::GetTempFileName()
$emptyNotesFile = [IO.Path]::GetTempFileName()
try {
    Set-Content -LiteralPath $notesFile -Value 'Hand-written release notes.'
    Set-Content -LiteralPath $emptyNotesFile -Value '   '
    foreach ($case in $cases) {
        $releaseTestState = @{
            StatusCode = $case.Status
            NetworkFailure = $case.NetworkFailure
            ExitCode = [int]$case.ExitCode
            Commands = [System.Collections.Generic.List[object]]::new()
            Lookups = 0
        }
        $caseNotesFile = switch ($case.Notes) {
            'Missing' { "$notesFile.missing" }
            'Empty' { $emptyNotesFile }
            default { $notesFile }
        }
        $failure = $null
        try {
            & "$PSScriptRoot/Publish-GitHubRelease.ps1" -Tag 'v0.1.0' -Prerelease:([bool]$case.Prerelease) -NotesFile $caseNotesFile
        } catch { $failure = $_.Exception.Message }
        if ($case.Error) {
            if (!$failure -or !$failure.Contains($case.Error)) { throw "Expected '$($case.Error)', got '$failure'." }
        } elseif ($failure) { throw $failure }
        if ($case.Command) {
            if ($releaseTestState.Commands.Count -ne 1 -or $releaseTestState.Commands[0][1] -ne $case.Command) {
                throw "Expected exactly one gh release $($case.Command) call."
            }
            foreach ($asset in 'tweens.gd-0.1.0.zip', 'tweens.gd-tutorial.zip', 'tweens.gd.0.1.0.nupkg', 'tweens.gd.0.1.0.snupkg') {
                if ($releaseTestState.Commands[0] -notcontains $asset) {
                    throw "Release publication must include $asset."
                }
            }
            if (($releaseTestState.Commands[0] -contains '--prerelease') -ne [bool]$case.Prerelease) {
                throw 'Incorrect prerelease option.'
            }
            if ($case.Command -eq 'create') {
                $command = $releaseTestState.Commands[0]
                $notesIndex = [array]::IndexOf($command, '--notes-file')
                if ($notesIndex -lt 0 -or $command[$notesIndex + 1] -ne $notesFile -or $command -contains '--generate-notes') {
                    throw 'Release creation must use the hand-written notes file.'
                }
                $titleIndex = [array]::IndexOf($command, '--title')
                if ($titleIndex -lt 0 -or $command[$titleIndex + 1] -cne 'v0.1.0') {
                    throw 'Release titles must match the tag without a project-name prefix.'
                }
            }
        } elseif ($releaseTestState.Commands.Count -ne 0) { throw 'Lookup failure must not publish anything.' }
        if ($case.Notes -and $releaseTestState.Lookups -ne 0) { throw 'Invalid notes must fail before contacting GitHub.' }
    }
} finally {
    Remove-Item -LiteralPath $notesFile, $emptyNotesFile
    $global:LASTEXITCODE = $savedExitCode
}
Write-Output "Passed $($cases.Count) release publishing checks."
