param([string]$ZigPath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$installerName=& "$PSScriptRoot/Get-InstallerName.ps1"
$dist=Join-Path $root 'dist'
$compiler=& "$PSScriptRoot/Initialize-Installer.ps1"
$stage=Join-Path $root ('artifacts/package-'+[Guid]::NewGuid().ToString('N'))
$native=Join-Path $root 'artifacts/native'
New-Item -ItemType Directory -Force $dist | Out-Null
& "$PSScriptRoot/Build.ps1" -Destination $stage -Production -Portable
& "$PSScriptRoot/Build-Native.ps1" -Destination $native -ZigPath $ZigPath
foreach($file in @('PipRender32.dll','PipRender64.dll','PipAttach32.exe','PipAttach64.exe','PipProbe32.exe','PipProbe64.exe','PipGpu.dll','LICENSE-MINHOOK.txt')){Copy-Item (Join-Path $native $file) $stage}
Copy-Item "$root/docs/USO.txt" "$stage/LEIA-ME.txt"
(Get-Content "$root/docs/en/USAGE.md" -Raw).Replace("](../pt-BR/USAGE.md)","](https://github.com/MaiconDAS/pip-overlay/blob/main/docs/pt-BR/USAGE.md)").Replace("](../USO.txt)","](https://github.com/MaiconDAS/pip-overlay/blob/main/docs/USO.txt)").Replace("](README.md)","](https://github.com/MaiconDAS/pip-overlay/blob/main/docs/en/README.md)").Replace("](../../PRIVACY.md)","](https://github.com/MaiconDAS/pip-overlay/blob/main/PRIVACY.md)").Replace("](COMPATIBILITY.md)","](https://github.com/MaiconDAS/pip-overlay/blob/main/docs/en/COMPATIBILITY.md)").Replace("](SIGNING.md)","](https://github.com/MaiconDAS/pip-overlay/blob/main/docs/en/SIGNING.md)") | Set-Content "$stage/READ-ME.md"
Copy-Item "$root/LICENSE" "$stage/LICENSE"
(Get-Content "$root/PRIVACY.md" -Raw).Replace("](docs/", "](https://github.com/MaiconDAS/pip-overlay/blob/main/docs/").Replace("](SECURITY.md)", "](https://github.com/MaiconDAS/pip-overlay/blob/main/SECURITY.md)") | Set-Content "$stage/PRIVACY-en.md"
(Get-Content "$root/docs/pt-BR/PRIVACY.md" -Raw).Replace("](../../PRIVACY.md)", "](https://github.com/MaiconDAS/pip-overlay/blob/main/PRIVACY.md)").Replace("](../README.md)", "](https://github.com/MaiconDAS/pip-overlay/blob/main/docs/README.md)").Replace("](SECURITY.md)", "](https://github.com/MaiconDAS/pip-overlay/blob/main/docs/pt-BR/SECURITY.md)") | Set-Content "$stage/PRIVACY-pt-BR.md"
Copy-Item "$root/THIRD_PARTY_NOTICES.md" "$stage/THIRD_PARTY_NOTICES.md"
Copy-Item (Join-Path (Split-Path $compiler) "license.txt") "$stage/LICENSE-INNO-SETUP.txt"
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
& $compiler "/DStage=$stage" "$root/installer/PipOverlay.iss"
if($LASTEXITCODE){throw 'Falha na compilação do instalador'}
Get-FileHash "$dist/installer/$installerName" -Algorithm SHA256 | ForEach-Object {"$($_.Hash.ToLower())  $installerName"} | Set-Content "$dist/SHA256SUMS.txt"
Write-Output "Instalador pronto em $dist/installer/$installerName"
