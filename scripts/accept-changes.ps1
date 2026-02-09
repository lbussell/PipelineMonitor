#!/usr/bin/env pwsh
#
# Copyright (c) 2026 Logan Bussell. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
# Based on https://github.com/dotnet/dotnet-docker/blob/e029e4f279a250f4c68899fc3d4e04b7c085e15f/tests/accept-changes.ps1
#

<#
.SYNOPSIS
    Accepts or discards changes in baseline files for .NET Docker tests.

.DESCRIPTION
    This script processes baseline files in the Microsoft.DotNet.Docker.Tests/Baselines directory.
    If the -Discard switch is specified, it deletes all .received.txt files.
    Otherwise, it renames all .received.txt files to .approved.txt.

.PARAMETER Discard
    If specified, the script will discard (delete) all .received.txt files.
    If not specified, the script will rename all .received.txt files to .approved.txt.

.EXAMPLE
    .\accept-changes.ps1
    This will rename all .received.txt files to .approved.txt in the Baselines directory.

.EXAMPLE
    .\accept-changes.ps1 -Discard
    This will delete all .received.txt files in the Baselines directory.
#>

param ([switch]$Discard)

$files = Get-ChildItem `
    -Recurse `
    -Path "$PSScriptRoot/../src/AzurePipelinesTool.Tests/Display" `
    -Filter "*.received.txt"

if ($Discard) {
    $files | Remove-Item
} else {
    $files | Move-Item -Force -Destination { $_.DirectoryName + "/" + ($_.Name -replace '\.received\.txt$', '.verified.txt') }
}
