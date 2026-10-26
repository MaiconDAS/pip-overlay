param([string]$Destination,[string]$ZigPath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Destination){$Destination=Join-Path $root 'artifacts/app'}
if(!$ZigPath){$ZigPath=Join-Path $root '.tools/zig-x86_64-windows-0.14.1/zig.exe'}
if(!(Test-Path $ZigPath)){throw 'Execute scripts/Initialize-Tools.ps1 ou informe -ZigPath.'}
$env:ZIG_GLOBAL_CACHE_DIR=Join-Path $root '.cache/zig-global'
$env:ZIG_LOCAL_CACHE_DIR=Join-Path $root '.cache/zig-local'
$mh=Join-Path $root 'vendor/minhook'
$native=Join-Path $root 'native'
New-Item -ItemType Directory -Force $Destination | Out-Null
foreach($arch in @('x86','x86_64')){
    $bits=if($arch -eq 'x86'){32}else{64}
    $common=@('-target',"$arch-windows-gnu",'-O2','-I',"$mh/include")
    & $ZigPath c++ @common -shared "$native/renderer.cpp" "$mh/src/buffer.c" "$mh/src/hook.c" "$mh/src/trampoline.c" "$mh/src/hde/hde32.c" "$mh/src/hde/hde64.c" -ld3d9 -luser32 -o "$Destination/PipRender$bits.dll"
    if($LASTEXITCODE){throw "Falha renderer $bits"}
    & $ZigPath c++ @common "$native/loader.cpp" -municode -o "$Destination/PipAttach$bits.exe"
    if($LASTEXITCODE){throw "Falha loader $bits"}
    & $ZigPath c++ @common "$native/host.cpp" -mwindows -ld3d9 -luser32 -o "$Destination/PipTest$bits.exe"
    if($LASTEXITCODE){throw "Falha host $bits"}
    & $ZigPath c++ @common -I "$native" "$native/addresses.cpp" "$native/probe.rc" -ld3d9 -luser32 -o "$Destination/PipProbe$bits.exe"
    if($LASTEXITCODE){throw "Falha probe $bits"}
}
& $ZigPath c++ -target x86_64-windows-gnu -O2 -shared "$native/capture.cpp" -ld3d11 -ld3dcompiler_47 -luser32 -o "$Destination/PipGpu.dll"
if($LASTEXITCODE){throw 'Falha captura GPU'}
Copy-Item "$mh/LICENSE.txt" "$Destination/LICENSE-MINHOOK.txt" -Force
