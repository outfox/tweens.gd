# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
# Compatibility entry point. Both languages now ship in the same addon.
param([string] $Version, [string] $OutputDirectory = 'artifacts/packages')
& "$PSScriptRoot/Pack-Addon.ps1" -Version $Version -OutputDirectory $OutputDirectory
