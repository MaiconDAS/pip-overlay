# Validation 0.15.0 — interface and capture

[Português](../VALIDATION-0.15.md) · [Documentation](README.md)

> Historical record. For the current implementation, see [0.16 validation](VALIDATION.md).

Local tests on October 29, 2026 used the project's synthetic windows and renderers. No global Windows settings were changed.

## Interface

- Settings split into Imagem, Posição and Mais in a 316-logical-pixel sidebar.
- The editor retains 506 px height and 778 px width with the sidebar open at default window size.
- All three sections were also checked at the minimum 1040 × 720 size without horizontal scrolling.
- FPS selection, dropdown opening and enabling the adaptive minimum were checked in the workspace test.
- Selectors, toggles, buttons and margin controls use custom painting; keyboard navigation uses WinForms controls.
- Independent resolution/FPS, shortcuts and geometry remain covered by existing tests.

## Capture and performance

Capture waits for new-frame events and keeps only the most recent available frames. Maintenance every 250 ms keeps a static image alive and publishes pending readbacks. Transport still uses shared memory and asynchronous readback; it is not zero-copy. The required submission Flush remains.

Single-pass synthetic before/after comparison, video limited to 30 fps:

| Measurement | Before | After |
| --- | ---: | ---: |
| Published frames/s | 29.66 | 29.57 |
| Renderer FPS with overlay | 60.14 | 59.78 |
| Frame-time p95 | 17.69 ms | 18.70 ms |
| Producer CPU, % of one core | 21.00% | 13.31% |
| Mean submission time, including readback | 0.229 ms | 0.447 ms |

No-overlay phases also varied substantially: before-run CPU ranged from 17.24% to 5.04%; after-run CPU from 3.77% to 14.51%. **This comparison does not demonstrate a consistent CPU, latency or game-impact reduction.** Mean submission cost increased in this sample. The confirmed useful result is maintaining expected cadence while avoiding repeated processing when no new image arrives. Raw data: [before.json](../benchmarks/0.15/before.json), [after.json](../benchmarks/0.15/after.json).

No real game or the user's browser workload was tested in this version. The shared-GPU-texture experiment had not established a benefit and was not activated in the product.

## Renderers and image

- DirectX 11/12, OpenGL and Vulkan on x86/x64: pixel checks without mismatches.
- Native capture at 1280×720 and 1920×1080: exact pixels in the capture test.
- Continuous D3D9 capture passed.
- Synthetic results do not extend declared compatibility to every game or anticheat system.

## Distribution

Test tools are excluded from production. Public installer signing remains pending, following the owner's decision to prepare the process and await a certificate. Installation, shortcuts, registry and uninstallation are exercised only on disposable GitHub Actions Windows.

Static image: over 5.01 seconds, 17 maintenance wakeups, no newly processed frames and 274 presentations of the preserved image. CPU counter resolution does not justify claiming zero usage. [Report](../benchmarks/0.15/static.json).
