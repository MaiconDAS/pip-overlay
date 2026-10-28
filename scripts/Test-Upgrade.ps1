param([Parameter(Mandatory)][string]$Installer)
# Upgrade is now exercised with real install/uninstall only on a disposable runner.
& "$PSScriptRoot/Test-InstallerIntegration.ps1" -Installer $Installer
