param([string]$ZigPath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$app=Join-Path $root 'artifacts/tests'
if(!$ZigPath){$ZigPath=Join-Path $root '.tools/zig-x86_64-windows-0.14.1/zig.exe'}
& "$PSScriptRoot/Test.ps1"
& "$PSScriptRoot/Build-Native.ps1" -Destination $app -ZigPath $ZigPath
Copy-Item "$root/effects" $app -Recurse -Force
& "$app/PipOverlay.exe" --quality-test "$app/quality.txt" | Out-Null
if($LASTEXITCODE){throw 'Quality tests failed'}
Get-Content "$app/quality.txt"
& $ZigPath c++ -target x86_64-windows-gnu -O2 "$root/native/capture_test.cpp" -ld3d11 -luser32 -o "$app/PipCaptureTest.exe"
if($LASTEXITCODE){throw 'Capture test compilation failed'}
& "$app/PipCaptureTest.exe"
if($LASTEXITCODE){throw 'Pixel preservation test failed'}
$tools=Join-Path $root '.tools/msvc'
$msvc=Join-Path $tools 'Contents/VC/Tools/MSVC/14.44.35207'
$sdk=Join-Path $tools 'microsoft.windows.sdk.cpp/c/Include/10.0.26100.0'
$previousInclude=$env:INCLUDE;$previousLib=$env:LIB
try{
    $env:INCLUDE="$msvc/include;$sdk/ucrt;$sdk/shared;$sdk/um"
    foreach($arch in @('x64','x86')){
        $bits=if($arch -eq 'x64'){64}else{32}
        $zigArch=if($bits -eq 64){'x86_64'}else{'x86'}
        $libs=Join-Path $tools "microsoft.windows.sdk.cpp.$arch/c"
        $env:LIB="$msvc/lib/$arch;$libs/um/$arch;$libs/ucrt/$arch"
        & "$msvc/bin/Hostx64/$arch/cl.exe" /nologo /std:c++17 /EHsc /MT /O2 "$root/native/host_modern.cpp" "/Fo$app/host_modern$bits.obj" "/Fe$app/PipModern$bits.exe" /link /SUBSYSTEM:WINDOWS d3d11.lib d3d12.lib dxgi.lib dxguid.lib opengl32.lib gdi32.lib user32.lib
        if($LASTEXITCODE){throw "Modern host compilation failed: $arch"}
        & $ZigPath c++ -target "$zigArch-windows-gnu" -std=c++17 -O2 -mwindows -I "$root/vendor/vulkan/include" "$root/native/host_vulkan.cpp" -luser32 -o "$app/PipVulkanTest$bits.exe"
        if($LASTEXITCODE){throw "Vulkan host compilation failed: $arch"}
        foreach($api in @('d3d11','d3d12','opengl','vulkan')){
            $hostFile=if($api -eq 'vulkan'){"$app/PipVulkanTest$bits.exe"}else{"$app/PipModern$bits.exe"}
            $report="$app/multi-$api-$bits.json"
            & "$app/PipOverlay.exe" --multi-test $hostFile $report $api | Out-Null
            if($LASTEXITCODE){throw "Graphics test failed: $api $bits; see $report"}
            $data=Get-Content $report -Raw | ConvertFrom-Json
            Write-Output "$api $bits passed: $($data.samples[-1].verified) pixel checks, $($data.samples[-1].mismatched) mismatches"
        }
    }
}finally{$env:INCLUDE=$previousInclude;$env:LIB=$previousLib}
& "$app/PipOverlay.exe" --gpu-test "$app/capture-continuous.json" | Out-Null
if($LASTEXITCODE){throw 'Continuous capture test failed'}
Write-Output 'Graphics validation passed. Reports are in artifacts/tests.'
