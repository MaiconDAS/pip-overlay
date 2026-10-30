param(
    [string]$SignTool,
    [ValidatePattern('^[A-Fa-f0-9]{40}$')][string]$Thumbprint,
    [uri]$TimestampUrl,
    [switch]$CheckOnly
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$installerName=& "$PSScriptRoot/Get-InstallerName.ps1"
$installer=Join-Path $root ("dist/installer/"+$installerName)
if(!(Test-Path -LiteralPath $installer)){throw 'Build the installer first.'}
if($CheckOnly){
    $signature=Get-AuthenticodeSignature -LiteralPath $installer
    [pscustomobject]@{Installer=$installer;Signature=$signature.Status.ToString();Signing='Pending: verified code-signing certificate and SignTool required'}
    return
}
if(!$SignTool -or !(Test-Path -LiteralPath $SignTool) -or !$Thumbprint -or !$TimestampUrl){throw 'Provide SignTool, Thumbprint and TimestampUrl. Use -CheckOnly for read-only inspection.'}
if($TimestampUrl.Scheme -notin @('http','https')){throw 'Use the certificate provider RFC 3161 HTTP(S) timestamp endpoint.'}
$certificate=Get-Item -LiteralPath "Cert:\CurrentUser\My\$Thumbprint"
if(!$certificate.HasPrivateKey -or $certificate.NotBefore -gt (Get-Date) -or $certificate.NotAfter -lt (Get-Date)){throw 'A currently valid certificate with its private key is required.'}
if('1.3.6.1.5.5.7.3.3' -notin @($certificate.EnhancedKeyUsageList | ForEach-Object {$_.ObjectId.Value})){throw 'Certificate is not enabled for code signing.'}
# Work on a separate copy; never replace the unsigned release on failure.
$output=Join-Path $root ('artifacts/signed-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$signed=Join-Path $output $installerName
Copy-Item -LiteralPath $installer -Destination $signed
& $SignTool sign /s My /sha1 $Thumbprint /fd SHA256 /tr $TimestampUrl.AbsoluteUri /td SHA256 $signed
if($LASTEXITCODE){throw "Signing failed. Do not publish $output"}
& $SignTool verify /pa /all /v $signed
if($LASTEXITCODE){throw "Signature verification failed. Do not publish $output"}
$signature=Get-AuthenticodeSignature -LiteralPath $signed
if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $Thumbprint -or !$signature.TimeStamperCertificate){throw 'Signature, publisher or timestamp verification failed. Do not publish this copy.'}
$hash=(Get-FileHash -LiteralPath $signed -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $installerName" | Set-Content (Join-Path $output 'SHA256SUMS.txt')
Write-Output "Verified signed release: $output"
