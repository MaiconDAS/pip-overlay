param([string]$ZigPath,[string]$NativeDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$NativeDirectory){$NativeDirectory=Join-Path $root 'artifacts/tests'}
if(!$ZigPath){throw 'Provide the path to an existing Zig compiler. No system installation is performed.'}
$app=Join-Path $root ('artifacts/performance-'+[guid]::NewGuid().ToString('N'))
& "$PSScriptRoot/Build.ps1" -Destination $app
foreach($file in 'PipGpu.dll','PipTest32.exe','PipAttach32.exe','PipRender32.dll','PipProbe32.exe'){Copy-Item (Join-Path $NativeDirectory $file) $app}
$baseline=Join-Path $app 'baseline.dll';Copy-Item "$app/PipRender32.dll" $baseline
$source=Get-Content "$root/native/renderer.cpp" -Raw
$copy='for(UINT row=0;row<height;row++)memcpy((char*)rect.pBits+row*rect.Pitch,memory->pixels+row*width*4,width*4);'
if(!$source.Contains($copy)){throw 'Renderer changed; review candidate before benchmarking'}
$source.Replace($copy,'if(rect.Pitch==(INT)(width*4))memcpy(rect.pBits,memory->pixels,(size_t)width*height*4);else '+$copy) | Set-Content "$app/candidate.cpp"
$env:ZIG_GLOBAL_CACHE_DIR=Join-Path $root '.cache/zig-global';$env:ZIG_LOCAL_CACHE_DIR=Join-Path $root '.cache/zig-local'
$mh=Join-Path $root 'vendor/minhook'
& $ZigPath c++ -target x86-windows-gnu -O2 -I "$mh/include" -I "$root/native" -shared "$app/candidate.cpp" "$mh/src/buffer.c" "$mh/src/hook.c" "$mh/src/trampoline.c" "$mh/src/hde/hde32.c" "$mh/src/hde/hde64.c" -ld3d9 -luser32 -o "$app/candidate.dll"
if($LASTEXITCODE){throw 'Candidate compilation failed'}
$cases=@(@{fps=30;res='1080p'},@{fps=60;res='1080p'},@{fps=60;res='720p'})
try{
    for($round=0;$round -lt 3;$round++){for($i=0;$i -lt 3;$i++){
        $c=$cases[($round+$i)%3]
        & "$app/PipOverlay.exe" --performance-test "$app/matrix-$round-$($c.res)-$($c.fps).json" $c.fps $c.res | Out-Null
        if($LASTEXITCODE){throw 'Configuration benchmark failed'}
    }}
    $n=0
    foreach($variant in @('baseline','candidate','unpaced','unpaced','candidate','baseline')){
        $library=if($variant -eq 'candidate'){"$app/candidate.dll"}else{$baseline}
        Copy-Item $library "$app/PipRender32.dll" -Force
        $extra=if($variant -eq 'unpaced'){@('--unpaced')}else{@()}
        & "$app/PipOverlay.exe" --performance-test "$app/ab-$n-$variant.json" 60 1080p @extra | Out-Null
        if($LASTEXITCODE){throw 'Alternative benchmark failed'}
        $n++
    }
}finally{Copy-Item $baseline "$app/PipRender32.dll" -Force}
Write-Output "Reports: $app. Synthetic D3D9 host; CPU includes the animated source. Inspect source FPS and both baseline phases before attributing gains. Unpaced is diagnostic, not a release feature."
