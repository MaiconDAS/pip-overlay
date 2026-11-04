# Validation 0.17

[Português](../VALIDATION-0.17.md) · **English** · [Documentation](README.md)

Tested on November 4, 2026, Windows x64 with Intel UHD Graphics 770. Dedicated synthetic sources and applications ran inside the project. No global Windows settings were changed.

## Functionality

- Ten languages: complete catalog entries, format parameters, persistence, live switching and editor rendering. Translation does not restart capture.
- Already-running DirectX 11, x86/x64: three positions, 1080p/720p capture, rounded corners, swapchain resizing, graphics-state restoration and CPU fallback. Sixteen pixel checks without mismatches per test.
- Prepared DirectX 11/12, OpenGL and Vulkan integrations, x86/x64: pixel checks passed. Native-size 720p/1080p capture preserves synthetic source pixels.
- Independent output: correct image, click-through hit testing, no overlay activation or foreground event, and resizing. The test observes Windows foreground events.
- Bounded automatic reconnect; pausing cancels retries. Existing profile, shortcut, geometry and quality checks remain covered.

Independent output supports windowed/borderless. Exclusive fullscreen requires renderer integration. The initial D3D11 fullscreen test was refused by Windows before attachment (0x887A0022); windowed results therefore do not certify fullscreen. These are dedicated-host results, not validation of Black Desert Online or all games.

## Performance

Animated Full HD source, 60-fps target, three runs per backend in prepared/direct/direct/prepared/prepared/direct order. Both use shared GPU transport. Medians:

| Metric | Prepared integration | Direct D3D11 attachment |
| --- | ---: | ---: |
| Frames uploaded to renderer/s | 42.90 | 42.66 |
| Host CPU, one-core equivalent | 12.20% | 11.12% |
| Producer CPU, including synthetic source | 88.53% | 86.68% |
| Submission time | 0.835 ms | 0.894 ms |

No FPS improvement was demonstrated. Run-to-run variation prevents promising a general cost reduction. The confirmed benefit is connection without restarting the application. Independent output avoids game integration and presents only new frames; real-game cost still depends on hardware, browser and scene.

Across 24 reconnects alternating 720p and 1080p, GPU transport stayed active. With garbage collection normalized before the baseline, private memory grew 24.74 MiB; samples every four cycles were approximately 15.1 / 16.4 / 16.6 / 22.1 / 24.0 / 24.7 MiB. This does not prove absence of leaks during long sessions. Earlier eight-cycle measurements used another baseline and are not a valid before/after comparison.

[Numeric summary](../benchmarks/0.17/summary.json) · [Reconnects](../benchmarks/0.17/reconnect.json)

## Reproduction and limits

Run `scripts/Test.ps1`, `scripts/Test-Graphics.ps1` and `scripts/Test-UX.ps1` using project-local tools. Installer integration tests that modify shortcuts and registry entries are allowed only on a disposable Windows runner. AMD/NVIDIA, multiple adapters and protected games still require specific validation. Public code signing remains pending.
