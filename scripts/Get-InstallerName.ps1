param([string]$Version)
$ErrorActionPreference='Stop'
if(!$Version){
    [xml]$project=Get-Content (Join-Path (Split-Path $PSScriptRoot -Parent) 'src/PipOverlay.csproj')
    $Version=[string]($project.Project.PropertyGroup.Version | Where-Object {$_} | Select-Object -First 1)
}
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw 'Expected a three-part numeric release version.'}
"PiP-Overlay v$Version.exe"
