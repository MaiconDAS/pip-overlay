# Releases and distribution

[Português](../RELEASES.md) · **English**

The current release is **0.19.0**, promoted from candidate on November 11, 2026, preserving the November 6 artifacts. [Download the current release](https://github.com/MaiconDAS/pip-overlay/releases/latest). See [evidence and remaining validation](VALIDATION-0.19.md) before interpreting the release status as a compatibility claim.

## Release archive

Older releases are historical, not separately maintained branches. The previous stable release, 0.18.0, remains available for manual rollback. Back up preferences first; backward compatibility of newer preferences is not guaranteed. Original filenames are preserved to avoid breaking download links.

| Version | Installer | Channel |
| --- | --- | --- |
| [v0.19.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.19.0) | `PiP-Overlay-v0.19.0.exe` | Current |
| [v0.18.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.18.0) | `PiP-Overlay-v0.18.0.exe` | Previous stable |
| [v0.17.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.17.0) | `PiP-Overlay-v0.17.0.exe` | Previous stable |
| [v0.16.1](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.16.1) | `PiP-Overlay-v0.16.1.exe` | Previous stable |
| [v0.16.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.16.0) | `Instalar-PiP-Overlay.exe` | Previous stable |
| [v0.15.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.15.0) | `Instalar-PiP-Overlay.exe` | Historical prerelease |
| [v0.14.1](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.14.1) | `Instalar-PiP-Overlay.exe` | Historical prerelease |
| [v0.14.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.14.0) | `Instalar-PiP-Overlay.exe` | Historical prerelease |
| [v0.13.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.13.0) | `Instalar-PiP-Overlay.exe` | Historical prerelease |
| [v0.12.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.12.0) | `Instalar-PiP-Overlay.exe` | Historical prerelease |
| [v0.11.1](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.11.1) | `Instalar-PiP-Overlay.exe` | Historical prerelease |
| [v0.11.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.11.0) | `Instalar-PiP-Overlay.exe` | Historical prerelease |
| [v0.10.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.10.0) | `Instalar-PiP-Overlay.exe` | Historical prerelease |

## Publication rules

1. Align the source version, changelog, paired Portuguese/English documentation and installer filename (`PiP-Overlay-vX.Y.Z.exe`).
2. Record what was actually checked and what remains unverified. Compile and validate code changes appropriately; test installer/system changes on disposable Windows before distribution. Never use the user's Windows as the first test environment.
3. Publish from a fixed source commit/tag with the installer and `SHA256SUMS.txt`. Preserve published tags and binaries; changed binaries require a new version.
4. Use prerelease for candidates. Promotion updates metadata only; record its date and decision separately from the original publication date. The app's updater uses stable releases.
5. Keep release notes bilingual and link compatibility, privacy, evidence and changelog. Release status does not imply support for every game, GPU or exclusive fullscreen.

SHA-256 verifies integrity, not publisher identity. Public signing is still pending. Downloads are not executed automatically by the app. Report problems through [Issues](https://github.com/MaiconDAS/pip-overlay/issues/new/choose); disclose vulnerabilities through [private reporting](https://github.com/MaiconDAS/pip-overlay/security/advisories/new).
