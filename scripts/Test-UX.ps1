$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$app=Join-Path $root 'artifacts/ux-tests'
& "$PSScriptRoot/Build.ps1" -Destination $app
# Requires an interactive desktop with DWM; uses only synthetic source windows.
& "$app/PipOverlay.exe" --ux-test "$app/ux.txt" | Out-Null
if($LASTEXITCODE){throw 'UX tests failed'}
Get-Content "$app/ux.txt"
