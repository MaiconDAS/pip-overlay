param([string]$Destination)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Destination){$Destination=Join-Path $root ('artifacts/install-test-'+[Guid]::NewGuid().ToString('N'))}
$Destination=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $Destination){throw 'Choose a new isolated test directory'}
$installer=Join-Path $root 'dist/installer/Instalar-PiP-Overlay.exe'
& $installer --extract $Destination | Out-Null
if($LASTEXITCODE){throw 'Installer extraction failed'}
$zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $root 'dist/PiP-Overlay-Windows-x64-portatil.zip'))
$count=0
try{
    foreach($entry in $zip.Entries){
        if(!$entry.Name){continue}
        $path=Join-Path $Destination $entry.FullName
        $inputStream=$entry.Open();$disk=[IO.File]::OpenRead($path)
        try{
            $hash1=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($inputStream))
            $hash2=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($disk))
            if($hash1 -ne $hash2){throw "Content mismatch: $($entry.FullName)"}
        }finally{$inputStream.Dispose();$disk.Dispose()}
        if($entry.FullName -match 'PipTest|PipModern|PipVulkanTest|PipCaptureTest|(^|/)data/|\.obj$|\.exp$|\.lib$'){throw "Development or user file leaked: $($entry.FullName)"}
        $count++
    }
}finally{$zip.Dispose()}
& $installer --extract $Destination | Out-Null
if($LASTEXITCODE -eq 0){throw 'Installer did not refuse an existing directory'}
$assembly=[Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes((Join-Path $Destination 'PipOverlay.dll')))
foreach($name in @('QualityTests','MultiApiTests','GpuValidation','DemoSource','RtssTest')){if($assembly.Contains($name)){throw "Development type leaked: $name"}}
Write-Output "PASS: $count extracted files match the package; existing destination refused; no developer tools or user settings."
Write-Output "Isolated test directory: $Destination"
