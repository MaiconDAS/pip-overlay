param([Parameter(Mandatory)][string]$Installer)
$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted'){
    throw 'This test modifies Windows integration and must run only on a disposable GitHub-hosted Windows runner.'
}
$Installer=(Resolve-Path -LiteralPath $Installer).Path
$root=Join-Path $env:RUNNER_TEMP ('pip-install-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$target=Join-Path $root 'Aplicativo com espaços'
$previous=Join-Path $root 'previous'
New-Item -ItemType Directory -Path "$previous/data/profiles" -Force | Out-Null
'{"Resolution":"1080p","FramesPerSecond":30}' | Set-Content "$previous/data/settings.json"
$key='A'*64
'{"Resolution":"720p","Fps":30}' | Set-Content "$previous/data/profiles/$key.json"
$registry='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{BFAFEE61-6519-4912-B741-389AC78FA812}_is1'
$desktop=Join-Path ([Environment]::GetFolderPath('Desktop')) 'PiP Overlay.lnk'
$menu=Join-Path ([Environment]::GetFolderPath('Programs')) 'PiP Overlay'
if((Test-Path $registry) -or (Test-Path $desktop) -or (Test-Path $menu)){throw 'Runner is not clean'}
function RunSetup {
    $p=Start-Process -FilePath $Installer -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/TASKS=desktopicon',('/DIR="'+$target+'"'),('/IMPORTFROM="'+$previous+'"'),('/LOG="'+$root+'/install.log"')) -WindowStyle Hidden -PassThru -Wait
    if($p.ExitCode -ne 0){throw "Install failed: $($p.ExitCode)"}
}
RunSetup
$entry=Get-ItemProperty $registry
if($entry.DisplayName -ne 'PiP Overlay' -or $entry.DisplayVersion -ne '0.14.0'){throw 'Wrong installed apps entry'}
if($entry.InstallLocation.TrimEnd('\') -ne $target){throw 'Wrong install location'}
$shell=New-Object -ComObject WScript.Shell
foreach($link in @($desktop,(Join-Path $menu 'PiP Overlay.lnk'))){
    if(!(Test-Path -LiteralPath $link)){throw "Missing shortcut: $link"}
    $shortcut=$shell.CreateShortcut($link)
    if($shortcut.TargetPath -ne "$target\PipOverlay.exe" -or $shortcut.WorkingDirectory -ne $target -or $shortcut.IconLocation -notlike '*PipOverlay.exe*'){throw 'Shortcut target or icon incorrect'}
}
foreach($link in @("$target/Desinstalar PiP Overlay.lnk","$menu/Desinstalar PiP Overlay.lnk")){if(!(Test-Path -LiteralPath $link)){throw 'Missing uninstall shortcut'}}
if((Get-FileHash "$previous/data/settings.json").Hash -ne (Get-FileHash "$target/data/settings.json").Hash){throw 'Import failed'}
if(!(Test-Path "$target/data/profiles/$key.json")){throw 'Profile import failed'}
'keep this user file' | Set-Content "$target/personal.txt"
'{"Resolution":"720p","FramesPerSecond":15}' | Set-Content "$target/data/settings.json"
$settingsHash=(Get-FileHash "$target/data/settings.json").Hash
RunSetup
if((Get-FileHash "$target/data/settings.json").Hash -ne $settingsHash){throw 'Update overwrote preferences'}
$uninstaller=Join-Path $target 'unins000.exe'
if($entry.UninstallString -notlike ('*'+$uninstaller+'*')){throw 'Uninstall entry does not point to uninstaller'}
$mutex=[Threading.Mutex]::new($false,'Local\PipOverlay-Portable-0.1')
try {
    $blocked=Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -PassThru -Wait
    if($blocked.ExitCode -eq 0 -or !(Test-Path "$target/PipOverlay.exe")){throw 'Running application was not protected from uninstall'}
}finally{$mutex.Dispose()}
$p=Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+$root+'/uninstall.log"')) -WindowStyle Hidden -PassThru -Wait
if($p.ExitCode){throw 'Uninstall failed'}
if((Test-Path $registry) -or (Test-Path $desktop) -or (Test-Path "$menu/PiP Overlay.lnk") -or (Test-Path "$target/PipOverlay.exe")){throw 'Integration or app remained after uninstall'}
if(!(Test-Path "$target/personal.txt") -or (Get-FileHash "$target/data/settings.json").Hash -ne $settingsHash){throw 'Uninstaller deleted personal data'}
Write-Output 'PASS: real per-user install, desktop/start shortcuts and icons, Installed apps entry, import, update preserving preferences, uninstall and preservation of user data.'
