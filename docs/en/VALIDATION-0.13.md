# Validation 0.13.0

[Português](../VALIDATION-0.13.md) · [Documentation](README.md)

> Historical record. For the current implementation, see [0.16 validation](VALIDATION.md).

Local environment: Windows, Intel UHD Graphics 770. This release changes UI, profiles and messages, not production renderers.

## Interface and regression

- 13 checks in `scripts/Test-UX.ps1`: snapping, margin, center, free movement, screen limits, centering, undo, history on profile switch, old-profile loading, option copying, connection states, two DWM thumbnails, correct selection and rejection of a closed source. Some checks group conditions.
- Existing automatic connection, pause/resume, shortcut, geometry, adaptive controller, profile, diagnostic and update tests passed.
- Normal and expanded interface previews were inspected. Thumbnail tests use two synthetic windows; they do not validate multiple real videos in each browser.
- Interaction remains inside the tool. Ctrl+Z is editor-local; no new global shortcut was introduced.

## Extended capture cycle

The final test completed **30 cycles in 171.37 seconds**, alternating 640×360 and 1280×720 synthetic sources, 720p/1080p sessions at 30 fps and a change to 480p at 15 fps. Each cycle stopped capture and closed the source before creating another.

- 30 click messages received by the dedicated x86 D3D9 host.
- Zero unwanted changes to the graphics states being checked.
- Publishing and rendering confirmed in every cycle.
- Producer private-memory change between cycle 3 and the final cycle: +2,437,120 bytes (~2.32 MiB). This short sample does not prove absence of long-term leaks.
- Initial runs were interrupted when the host exited and were not counted as passing. The final run used a host rebuilt by the current native pipeline.

This exercises session/source disposal and recreation. It does not measure real-game FPS, simulate hours of use, or replace Edge/Chrome tests and physical clicks inside a game.

## Distribution and signing

- Production build and 0.13.0 installer completed.
- 493 files extracted into a temporary directory and hash-compared with the package. Existing destinations were rejected.
- No test modes, synthetic sources or personal preferences were packaged.
- Preference/profile import tested in temporary folders: source preserved, unrelated files ignored, invalid import rolled back.
- Installer verified as `NotSigned`. Signature inspection and rejection of incomplete parameters passed; real signing awaits a public certificate. See [signing](SIGNING.md).

## Limits

No real games or new game-FPS impact measurements in this round. AMD/NVIDIA, newer-API fullscreen and real-video validation remain pending. No promise of zero cost, guaranteed 60 fps or universal compatibility. See the [test matrix](TEST-MATRIX.md).

No Windows settings, drivers, services, registry entries or browser policies were changed.
