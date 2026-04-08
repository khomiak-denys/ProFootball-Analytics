$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

git config core.hooksPath .githooks
Write-Host "Git hooks path configured to .githooks"
