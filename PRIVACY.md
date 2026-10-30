# Privacy policy

[Português](docs/pt-BR/PRIVACY.md) · [Documentation](docs/en/README.md)

Updated: October 29, 2026. Applies to the official PiP Overlay desktop application maintained in [MaiconDAS/pip-overlay](https://github.com/MaiconDAS/pip-overlay). Forks and external websites may behave differently.

## Local video processing

The application identifies local browser PiP windows and game processes to establish the overlay. It can inspect window titles, process names, identifiers and the selected executable path locally. It captures pixels from the selected PiP window and passes frames to the renderer on the same computer through RAM or GPU resources. The application does not implement video recording, audio capture, cloud streaming, advertising or usage analytics. It does not read browser passwords, cookies, browsing history or the DOM of ordinary tabs.

Frames are transient resources; the application does not intentionally save them as video or image files. Windows manages memory, paging and crash dumps independently. The PiP source window may be enlarged temporarily and restored when capture ends.

## Data saved on your computer

The installation's `data` folder stores preferences, monitor selection, geometry, quality/FPS, shortcuts, update-check timestamps and game profiles. Game selection and launch records can contain executable paths, process identifiers and launch times. Renderer configuration, caches and third-party technical logs may contain paths and system/graphics information. Generated Direct3D discovery files are local to this computer. These files are used for configuration, compatibility and troubleshooting, not uploaded by PiP Overlay.

Preferences and personal files are preserved when uninstalling. To remove local history/configuration, close PiP Overlay and games launched through it, then remove the remaining application `data` folder and any diagnostic files you saved, after backing up anything you want to keep. There is no project server holding a copy of these local settings.

## Network requests and GitHub

Update notifications are enabled by default. The app requests the public releases API at `api.github.com/repos/MaiconDAS/pip-overlay/releases`; its User-Agent includes the app version. GitHub receives ordinary connection information, including your IP address. This request does not include captured frames, browser titles, video URLs, executable paths or preferences. Automatic successful checks are limited to once per day on startup; manual checks can also be requested. Disable **Ajustes > Mais > Avisar atualizações** to stop automatic checks. Local overlay processing does not require this check.

Opening a release/download page uses your browser. Repository pages, issues, downloads and update infrastructure are operated by GitHub under its [privacy statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement). Your browser, video provider, Windows and graphics drivers have their own policies. Build tools download development dependencies separately; they are not part of normal installed-app operation.

## Diagnostics and support

**Diagnóstico** saves a JSON file only after you choose a destination. It includes app/Windows versions, process architecture, selected resolution/FPS, capture dimensions, transport status and performance/error counters. Its allowlist excludes frames, audio, URLs, window titles, personal paths and raw exception text. It is not sent automatically.

If you attach a report, screenshot or log to a GitHub issue, you choose to share that content; public issues are public. Review third-party logs and screenshots before sharing because they may contain identifying information. The sanitized JSON report and renderer logs are different files.

## Contact and changes

For general privacy questions, contact the repository maintainer through a [GitHub issue](https://github.com/MaiconDAS/pip-overlay/issues), without including private data. For vulnerabilities, follow [SECURITY.md](SECURITY.md). Requests about data held by GitHub should use GitHub's privacy contact channels. This policy is versioned with the code; material changes should be described in release notes. It documents current behavior and does not claim certification or universal legal compliance.
