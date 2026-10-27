param([string]$Destination)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Destination){$Destination=Join-Path $root 'artifacts/graphics'}
New-Item -ItemType Directory -Force $Destination | Out-Null
$download=Join-Path $root '.cache/ReShade_Setup_6.8.0_Addon.exe'
New-Item -ItemType Directory -Force (Split-Path $download) | Out-Null
if(!(Test-Path $download)){Invoke-WebRequest 'https://reshade.me/downloads/ReShade_Setup_6.8.0_Addon.exe' -OutFile $download}
if((Get-FileHash $download).Hash -ne 'AFE4C8F13048306307983B8B3D41D5BF00A86820440B0E57DEA10950E1176445'){throw 'ReShade checksum mismatch'}
# Read the embedded archive, as documented by the upstream setup source. Never run setup.
$bytes=[IO.File]::ReadAllBytes($download)
$offset=-1
for($i=0;$i -lt $bytes.Length-30;$i+=512){if($bytes[$i] -eq 80 -and $bytes[$i+1] -eq 75 -and $bytes[$i+2] -eq 3 -and $bytes[$i+3] -eq 4){$offset=$i;break}}
if($offset -lt 0){throw 'Embedded archive not found'}
$stream=[IO.MemoryStream]::new($bytes,$offset,$bytes.Length-$offset,$false)
$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read)
try{foreach($name in @('ReShade32.dll','ReShade64.dll','ReShade32.json','ReShade64.json')){
    $entry=$zip.GetEntry($name)
    if(!$entry){throw "Missing $name"}
    [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,(Join-Path $Destination $name),$true)
}}finally{$zip.Dispose();$stream.Dispose()}
Copy-Item "$root/vendor/reshade/LICENSE.md" "$Destination/LICENSE-RESHADE.md" -Force
