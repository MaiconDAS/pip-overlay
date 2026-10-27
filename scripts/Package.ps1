param([string]$ZigPath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$dist=Join-Path $root 'dist'
$stage=Join-Path $root ('artifacts/package-'+[Guid]::NewGuid().ToString('N'))
$native=Join-Path $root 'artifacts/native'
New-Item -ItemType Directory -Force $dist | Out-Null
& "$PSScriptRoot/Build.ps1" -Destination $stage -Production -Portable
& "$PSScriptRoot/Build-Native.ps1" -Destination $native -ZigPath $ZigPath
foreach($file in @('PipRender32.dll','PipRender64.dll','PipAttach32.exe','PipAttach64.exe','PipProbe32.exe','PipProbe64.exe','PipGpu.dll','LICENSE-MINHOOK.txt')){Copy-Item (Join-Path $native $file) $stage}
Copy-Item "$root/docs/USO.txt" "$stage/LEIA-ME.txt"
New-Item -ItemType Directory "$stage/graphics" | Out-Null
foreach($file in @('ReShade32.dll','ReShade64.dll','ReShade32.json','ReShade64.json','PipOverlay.addon32','PipOverlay.addon64','PipVulkan32.dll','PipVulkan64.dll','PipLaunch32.exe','PipLaunch64.exe','LICENSE-RESHADE.md')){Copy-Item "$native/graphics/$file" "$stage/graphics/$file"}
Copy-Item "$root/vendor/vulkan/LICENSE.md" "$stage/graphics/LICENSE-VULKAN-HEADERS.md"
Copy-Item "$root/effects" "$stage/effects" -Recurse
$runtime=Get-ChildItem "$env:NUGET_PACKAGES/microsoft.netcore.app.runtime.win-x64" -Directory | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
$desktop=Get-ChildItem "$env:NUGET_PACKAGES/microsoft.windowsdesktop.app.runtime.win-x64" -Directory | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
Copy-Item "$($runtime.FullName)/LICENSE.TXT" "$stage/LICENSE-DOTNET.txt"
Copy-Item "$($runtime.FullName)/THIRD-PARTY-NOTICES.TXT" "$stage/THIRD-PARTY-NOTICES-DOTNET.txt"
Copy-Item "$($desktop.FullName)/LICENSE" "$stage/LICENSE-WINDOWSDESKTOP.txt"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $dist 'PiP-Overlay-Windows-x64-portatil.zip'
if(Test-Path -LiteralPath $zip){Remove-Item -LiteralPath $zip}
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
dotnet publish "$root/installer/Installer.csproj" -c Release -r win-x64 --self-contained true --configfile "$root/NuGet.Config" -o "$dist/installer"
if($LASTEXITCODE){throw 'Falha na compilação do instalador'}
Get-FileHash "$dist/installer/Instalar-PiP-Overlay.exe" -Algorithm SHA256 | ForEach-Object {"$($_.Hash.ToLower())  Instalar-PiP-Overlay.exe"} | Set-Content "$dist/SHA256SUMS.txt"
Write-Output "Instalador pronto em $dist/installer/Instalar-PiP-Overlay.exe"
