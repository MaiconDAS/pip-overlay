param([string]$Destination)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Destination){$Destination=Join-Path $root ('artifacts/upgrade-test-'+[Guid]::NewGuid().ToString('N'))}
New-Item -ItemType Directory -Path "$Destination/old/data/profiles" -Force | Out-Null
'{"Resolution":"1080p","FramesPerSecond":30}' | Set-Content "$Destination/old/data/settings.json"
$key='A'*64
'{"Resolution":"720p","Fps":30}' | Set-Content "$Destination/old/data/profiles/$key.json"
'do not copy' | Set-Content "$Destination/old/data/unrelated.txt"
$oldHash=(Get-FileHash "$Destination/old/data/settings.json").Hash
& "$root/dist/installer/Instalar-PiP-Overlay.exe" --extract "$Destination/new" --from "$Destination/old" | Out-Null
if($LASTEXITCODE){throw 'Upgrade extraction failed'}
if((Get-FileHash "$Destination/new/data/settings.json").Hash -ne $oldHash){throw 'Settings not preserved'}
if((Get-FileHash "$Destination/old/data/settings.json").Hash -ne $oldHash){throw 'Old settings changed'}
if(!(Test-Path "$Destination/new/data/profiles/$key.json")){throw 'Profile not preserved'}
if(Test-Path "$Destination/new/data/unrelated.txt"){throw 'Unrelated data copied'}
'not json' | Set-Content "$Destination/old/data/settings.json"
& "$root/dist/installer/Instalar-PiP-Overlay.exe" --extract "$Destination/invalid" --from "$Destination/old" | Out-Null
if($LASTEXITCODE -eq 0 -or (Test-Path "$Destination/invalid")){throw 'Invalid settings were not rejected atomically'}
Write-Output 'PASS: preferences and profiles copied; old installation preserved; unrelated files excluded; invalid import rolled back.'
