# Validation 0.14.0

[Português](../VALIDATION-0.14.md) · [Documentation](README.md)

> Historical record. For the current implementation, see [0.16 validation](VALIDATION.md).

## Complete installer

The earlier extractor was replaced by Inno Setup 7.1.0. Default installation is per-user at `%LOCALAPPDATA%\Programs\PiP Overlay`, with another directory selectable and no elevation. Uninstall registration uses HKCU; no service, driver, scheduled task or automatic startup is created.

The `installer.yml` workflow tests a draft release executable on disposable GitHub Windows. The test explicitly refuses to run outside a GitHub-hosted runner.

Runner checks:

- Silent installation into a path containing spaces.
- Installed apps entry, name, version, directory and uninstall command.
- Desktop and Start menu shortcuts, target, working directory and icon.
- Uninstall shortcuts in the Start menu and application directory.
- Preference/profile import from an earlier portable installation.
- Reinstallation/update in the same directory without overwriting settings.
- Uninstallation refused while the application mutex indicates it is open.
- Removal of executable, shortcuts and uninstall registration.
- Preservation of preferences and a personal file placed in the directory.

## Package and icon

494 extracted files were SHA-256 compared with the prepared content. No test host or personal preferences were distributed. Connection, shortcut, geometry, profile and update regression tests passed.

The icon includes 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixel images. The executable and installer embed this ICO; the interface loads the appropriate size from the embedded resource. The interface preview was inspected.

## Limits and Windows protection

Integration was tested on the hosted runner's Windows, not the user's actual installation. This does not test every corporate policy, OneDrive profile or Windows 10/11 version. Renderers were unchanged; no new game-compatibility claim was made.

Public signing remains pending a certificate as requested by the owner. Published hashes establish integrity, not publisher identity.

The compiler was extracted from a verified official archive without running its installer. No DNS, registry, service or driver configuration on the user's computer was changed. The distributed installation is reversed through its uninstaller; personal files are preserved.
