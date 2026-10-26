param([string]$Destination,[switch]$Production,[switch]$Portable)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Destination){$Destination=Join-Path $root 'artifacts/app'}
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='false'
$env:DOTNET_CLI_HOME=Join-Path $root '.cache/dotnet'
if(!$env:NUGET_PACKAGES){$env:NUGET_PACKAGES=Join-Path $root '.cache/packages'}
$runtime=if($Portable){@('-r','win-x64','--self-contained','true')}else{@('--self-contained','false')}
dotnet publish "$root/src/PipOverlay.csproj" -c Release @runtime --configfile "$root/NuGet.Config" "-p:ProductionBuild=$($Production.IsPresent)" -o $Destination
if($LASTEXITCODE){throw 'Falha na compilação gerenciada'}
