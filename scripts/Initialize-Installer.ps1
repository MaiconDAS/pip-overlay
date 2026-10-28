$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$folder=Join-Path $root '.tools/inno'
$compiler=Join-Path $folder 'extracted/{app}/ISCC.exe'
if(Test-Path -LiteralPath $compiler){return $compiler}
New-Item -ItemType Directory -Force $folder | Out-Null
function DownloadChecked($url,$path,$hash){
    if(!(Test-Path -LiteralPath $path)){Invoke-WebRequest $url -OutFile $path}
    if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $hash){throw "Checksum mismatch: $path"}
}
$setup=Join-Path $folder 'innosetup-7.1.0-x64.exe'
DownloadChecked 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe' $setup '0362A383ED217D4C4239B5933866DD96D3EB2102737DA92F80F6057A4B40DF2F'
$zip=Join-Path $folder 'innounp.zip'
DownloadChecked 'https://raw.githubusercontent.com/jrathlev/InnoUnpacker-Windows-GUI/032d64c782efc6b36766d8f9c3b1b2f042144a58/innounp-2/bin/innounp-2.zip' $zip '10FCE27B6344B7674D5625CC34AFCA1C88C12183D957C6D93E401097FA2556CD'
Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $folder 'unpacker') -Force
# Extract archives only. Never execute the compiler installer or register it in Windows.
& "$folder/unpacker/innounp.exe" -x -y "-d$folder/extracted" $setup | Out-Null
if($LASTEXITCODE -or !(Test-Path -LiteralPath $compiler)){throw 'Compiler archive extraction failed'}
return $compiler
