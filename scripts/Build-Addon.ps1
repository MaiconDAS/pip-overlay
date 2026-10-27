param([string]$Destination)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$tools=Join-Path $root '.tools/msvc'
$msvc=Join-Path $tools 'Contents/VC/Tools/MSVC/14.44.35207'
if(!(Test-Path "$msvc/bin/Hostx64/x64/1033/clui.dll") -or !(Test-Path "$msvc/lib/x64/oldnames.lib")){
    New-Item -ItemType Directory -Force $tools | Out-Null
    foreach($package in (Get-Content "$PSScriptRoot/msvc-packages.json" -Raw | ConvertFrom-Json)){
        $archive=Join-Path $tools $package.name
        if(!(Test-Path $archive)){Invoke-WebRequest $package.url -OutFile $archive}
        if((Get-FileHash $archive).Hash -ne $package.sha256){throw "Checksum mismatch: $($package.name)"}
        [IO.Compression.ZipFile]::ExtractToDirectory($archive,(Join-Path $tools $package.destination),$true)
    }
}
$sdk=Join-Path $tools 'microsoft.windows.sdk.cpp/c/Include/10.0.26100.0'
$previousInclude=$env:INCLUDE;$previousLib=$env:LIB
try{
    $env:INCLUDE="$msvc/include;$sdk/ucrt;$sdk/shared;$sdk/um"
    foreach($arch in @('x64','x86')){
        $bits=if($arch -eq 'x64'){64}else{32}
        $libs=Join-Path $tools "microsoft.windows.sdk.cpp.$arch/c"
        $env:LIB="$msvc/lib/$arch;$libs/um/$arch;$libs/ucrt/$arch"
        & "$msvc/bin/Hostx64/$arch/cl.exe" /nologo /std:c++17 /EHsc /MT /O2 /LD /I "$root/vendor/reshade" "$root/native/multi_api.cpp" "/Fo$Destination/multi_api$bits.obj" "/Fe$Destination/PipOverlay.addon$bits" /link user32.lib
        if($LASTEXITCODE){throw "Add-on compilation failed: $arch"}
    }
}finally{$env:INCLUDE=$previousInclude;$env:LIB=$previousLib}
