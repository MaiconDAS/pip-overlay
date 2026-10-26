$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$tools=Join-Path $root '.tools'
$zip=Join-Path $tools 'zig-0.14.1.zip'
$sha='554f5378228923ffd558eac35e21af020c73789d87afeabf4bfd16f2e6feed2c'
New-Item -ItemType Directory -Force $tools | Out-Null
if(!(Test-Path $zip)){Invoke-WebRequest 'https://ziglang.org/download/0.14.1/zig-x86_64-windows-0.14.1.zip' -OutFile $zip}
if((Get-FileHash -LiteralPath $zip).Hash.ToLower() -ne $sha){throw 'SHA256 do Zig divergente. O arquivo não será executado.'}
Expand-Archive -LiteralPath $zip -DestinationPath $tools -Force
Write-Output (Join-Path $tools 'zig-x86_64-windows-0.14.1/zig.exe')
