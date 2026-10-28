$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$app=Join-Path $root 'artifacts/tests'
& "$PSScriptRoot/Build.ps1" -Destination $app
foreach($test in @('auto','hotkey','geometry','improvement')){
    & "$app/PipOverlay.exe" "--$test-test" "$app/$test.txt" | Out-Null
    if($LASTEXITCODE){throw "Falha no teste $test"}
    Get-Content "$app/$test.txt"
}
