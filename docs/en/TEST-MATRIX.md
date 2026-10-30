# Test matrix

[Português](../TEST-MATRIX.md) · [English index](README.md)

Results refer to Intel UHD Graphics 770, driver 32.0.101.7092. Dedicated test hosts are not certified games.

| Environment | D3D9 | D3D11 | D3D12 | OpenGL | Vulkan |
| --- | --- | --- | --- | --- | --- |
| Windowed hosts, Intel UHD 770 | x86 tested; x64 built | x86/x64 tested | x86/x64 tested | x86/x64 tested | x86/x64 SDR tested |
| Fullscreen dedicated host | Historical 0.11 validation | Pending | Pending | Pending | Pending |
| Real-game performance comparison | Not controlled | Pending | Pending | Pending | Pending |
| AMD / NVIDIA | Hardware unavailable | Hardware unavailable | Hardware unavailable | Hardware unavailable | Hardware unavailable |

For a game benchmark, record app version, GPU/driver, API, display mode, game/video dimensions and requested FPS. Use the same scene, FPS cap and power conditions. Repeat three rounds of 60 seconds without overlay, 60 with it and 60 without it again. Record frame-time median/p95, producer/game CPU and published/uploaded frames. Test corners, center, resize, pause/resume, resolution switches, focus changes and game clicks.

Local synthetic benchmarks are shorter smoke/comparison tests, not substitutes for that protocol. `Test-Graphics.ps1` checks rendering, `--shared-test` checks integrated D3D11 sharing, `Test-Performance.ps1` compares configurations and `Test-SharedGpu.ps1` isolates transport mechanics. Installer lifecycle tests run only on disposable hosted Windows.

Only attach reviewed technical data. Do not include captured frames, video URLs, window titles or personal paths in a shared diagnostic report. Current results: [validation](VALIDATION.md).
