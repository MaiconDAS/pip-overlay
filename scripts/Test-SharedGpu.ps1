param([string]$ZigPath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$ZigPath){$ZigPath=Join-Path $root '.tools/zig-x86_64-windows-0.14.1/zig.exe'}
$app=Join-Path $root 'artifacts/tests'
$env:ZIG_GLOBAL_CACHE_DIR=Join-Path $root '.cache/zig-global';$env:ZIG_LOCAL_CACHE_DIR=Join-Path $root '.cache/zig-local'
& $ZigPath c++ -target x86_64-windows-gnu -std=c++17 -O2 -municode "$root/native/shared_gpu_probe.cpp" -ld3d11 -ldxgi -o "$app/PipSharedProbe.exe"
if($LASTEXITCODE){throw 'Probe build failed'}
& "$app/PipSharedProbe.exe" | Tee-Object "$app/shared-gpu-probe.json"
if($LASTEXITCODE){throw 'GPU sharing probe failed'}
