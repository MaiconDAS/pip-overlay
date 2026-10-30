# Changelog

[Português](../../CHANGELOG.md) · [Documentation](README.md)

## 0.16.1 — bilingual repository and versioned installer

- Reorganized both READMEs: download and current changes first; usage, editor, compatibility and contribution follow.
- Full English editions of historical validation reports, performance investigation and changelog, with reciprocal language links and a documentation index.
- Corrected outdated game-discovery descriptions and grouped current instructions separately from historical reports.
- Versioned installer filename: `PiP-Overlay v0.16.1.exe`. Packaging, checksum, signing and isolated installer tests use the same version-derived name.
- No capture or rendering changes in this patch.

## 0.16.0 — GPU transport, edges and documentation

- DirectX 11 uses a three-shared-texture ring when compatible; failures fall back to CPU transport, including with a paused source.
- Integrated comparison using the same renderer and resolution, published without generalizing to all games or APIs.
- PiP reaches all four screen edges without being pushed away by the snapping margin.
- Subtle rounded corners in the editor and renderers.
- Privacy/security policies and principal documentation in Portuguese and English; both privacy editions accompany the installer.
- Reviewed current instructions, versions, links and compatibility limits. Earlier reports remain labeled historical.
- Window/renderer discovery without game-name filters; unprepared processes are explicitly shown as disconnected.

## 0.15.0 — editor focus and modern settings

- Settings in an Imagem / Posição / Mais sidebar, preserving editor height when expanded.
- Consistent dark selectors, rounded buttons, toggles and margin control, retaining standard-control keyboard navigation.
- Resolution and FPS remain independent. Minimum FPS is enabled only with automatic adjustment.
- Frame-arrival-driven capture, stale-frame discarding and preference for the newest completed readback without waiting for the GPU.
- Static images remain visible without reprocessing duplicate frames. Source discovery is suspended when paused and hidden.
- Diagnostics include processed/discarded frame and capture-wakeup counters.
- Documented synthetic measurements without claiming real-game FPS gains. Shared-texture transport remained experimental.

## 0.14.1 — automatic installation discovery

- Removed the import step that blocked progression when its field was empty.
- Installer automatically reuses the registered installation directory.
- Existing directories can be selected directly without an extra warning; preferences and profiles there are preserved.
- Choosing another destination automatically copies registered-installation preferences without replacing existing destination settings.
- Portable installations are updated by choosing their own folder. No unrestricted disk scan.
- New interactive wizard test reaches confirmation, alongside clean-install and update tests.

## 0.14.0 — complete installer and visual identity

- Complete installer: optional desktop shortcut selected by default, Start menu entries and Windows Installed apps / Programs and Features registration.
- Uninstall through Windows, the Start menu or the app-directory shortcut. Preferences and personal files are preserved.
- In-place updates retain settings. Per-user installation in AppData without administrator rights, service or automatic startup.
- New deep-blue/mint rounded icon, with nine sizes from 16 to 256 pixels for the executable, shortcuts and tray.

![PiP Overlay icon](../assets/app-icon.png)

## 0.13.0 — visual selection and positioning

- With multiple PiPs, the Vídeos button opens thumbnails for one-click source selection.
- Edge/center snapping, adjustable margin, centering and last-position undo. Hold Alt for free dragging; Ctrl+Z works only in the editor.
- Visible game profile name, reset to defaults and copying settings to another executable.
- Clearer states for waiting for video, waiting for a game, interrupted capture and unconfirmed connection.
- Signing and verification workflow prepared; public installer signing awaits a certificate.

See [0.13 validation](VALIDATION-0.13.md) and [signing](SIGNING.md).

## 0.12.0 — stability, profiles and maintenance

- Preserve the last frame under D3D9/Vulkan contention; a test reproduces the earlier defect and checks the fix.
- Avoid two intermediate textures for native-size BGRA capture.
- Optional automatic FPS with limits and gradual recovery, without changing resolution.
- Per-executable profiles for monitor, position, size, resolution and FPS.
- Optional GitHub release checks without automatic installation.
- Diagnostic export with allowed technical fields, without images or personal video information.
- Installer imports preferences/profiles while preserving the earlier directory.
- Test matrix and benchmark with FPS and frame-time p95.
- Cross-process D3D11 sharing prototype pixel-validated; not activated in the product without proven benefit.

## 0.11.1 — independent resolution/FPS and MIT license

- Select up to 1080p, 720p, 480p or overlay-size adaptation.
- Select 60, 30 or 15 fps separately; changing FPS does not lower resolution.
- Existing preferences migrated, preserving adaptive resolution and FPS when present.
- Open game button removed from the main screen; manual launch remains only in the tray menu.
- Original code licensed under MIT; license and third-party notices bundled with the installer.
- 19 checks covering combinations, persistence, migration and window restoration passed.

This release separates the controls; it does not include a specific fix for the reported 60 fps flicker.

## 0.11.0 — quality and new renderers

- Native-resolution capture up to 1080p in Maximum mode, targeting 60 fps; 30/15 fps modes remain available.
- Temporary source-PiP enlargement and restoration on stop; effective resolution visible in status.
- DirectX 11/12 and OpenGL with bundled engine/custom add-on; Vulkan with a custom layer enabled only in the launched process.
- Open game button and persistent executable selection without copying files to the game directory.
- Connection restored after reopening the tool while the same game process remains alive.
- Graphics tests covering position, size and pixel verification; documented per-API limits.

Experimental release: dedicated-host validation, not game or HDR certification or a 60 fps guarantee. No global Windows configuration changes.

## 0.10.0 — first release in this repository

- Standalone single-executable installer, without an extension or RTSS.
- Optional application launch when installation completes.
- Edge/Chrome native PiP as the video source.
- Collapsed settings and proportional editor.
- Automatic connection with persistent pause and configurable shortcuts.
- System tray operation while the application window is closed.
- Direct3D 9 renderer, GPU capture downscaling and adaptive resolution up to 1080p.

Experimental state. Improvements were developed before repository creation; this release starts Git history without fabricating earlier commits.
