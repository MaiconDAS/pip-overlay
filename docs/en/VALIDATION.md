# Validation and measured performance

[Português](../VALIDATION-0.16.md) · [English index](README.md)

## Current build: 0.16

Intel UHD Graphics 770, driver 32.0.101.7092, Windows x64. Dedicated synthetic sources/renderers were used; no global Windows settings were changed.

Integrated D3D11 x64 comparison: animated Full HD source, 60-fps target, confirmed 1920×1080 transfer in both modes. Three runs per transport in CPU/GPU/GPU/CPU/CPU/GPU order, with warm-up and roughly eight seconds per observation. Medians from the rounded-corner build without redundant CPU staging buffers in GPU mode:

| Metric | CPU transport | Shared GPU textures |
| --- | ---: | ---: |
| Source frames/s | 58.83 | 63.24 |
| Published frames/s | 36.58 | 48.68 |
| Frames uploaded to renderer texture/s | 34.83 | 48.68 |
| Renderer CPU, percent of one core | 31.67% | 10.05% |
| Producer CPU including synthetic source, percent of one core | 111.40% | 72.28% |
| Sampled submit time, including readback in CPU mode | 3.50 ms | 0.29 ms |

These results support enabling GPU transport for D3D11 with fallback. They are not end-to-end latency, GPU utilization or real-game FPS measurements. Source cadence/system load varied, and producer CPU includes drawing the synthetic source. Do not generalize percentages to every game or GPU. [Raw data](../benchmarks/0.16).

The initial single-texture implementation reduced delivered cadence and was rejected. The final three-texture ring avoids waiting for the other process and retires old frames. It avoids CPU readback/upload on the shared path but still performs GPU copies and synchronization.

## Correctness checks

D3D11, D3D12 and OpenGL x86/x64 passed center-pixel and clipped-corner checks at five positions/sizes. Vulkan x86/x64 passed pixel/corner checks using bounded clipped blit regions. D3D9 x86 passed dedicated rounded-corner, position and resize validation. Continuous capture remains covered.

D3D11 GPU transport passed x86/x64 activation, position, size, 1080p-to-720p changes and forced CPU fallback with a paused source, without mismatches in the checked pixels. Native-size CPU capture retained pixel-preservation checks at 1280×720 and 1920×1080. Editor tests cover exact screen corners, margin/center snapping, source selection, profiles and undo.

The transport improvement is D3D11-only. Other APIs retain CPU transport. Newer-API fullscreen and real-game performance remain subject to specific validation; windowed hosts do not prove them. AMD/NVIDIA and multi-adapter scenarios were not validated on this hardware.

Production excludes test classes/modes. Packages include both privacy-policy languages and English usage instructions. Publication requires package verification and installer lifecycle checks on disposable Windows. Public signing remains pending.

## Historical evidence

0.15 introduced the sidebar and event-driven capture, but its short before/after benchmark did not prove consistent CPU/game-FPS gains. Earlier releases validated modern API hosts, independent resolution/FPS, source selection, profiles and the installer lifecycle. Full historical reports are preserved in Portuguese and explicitly labeled historical; see the [Portuguese documentation index](../README.md).

The earlier isolated GPU-sharing microbenchmark compared against a synchronous CPU reference, not the released asynchronous transport. Its roughly 75% reduction was not a product-wide claim. Current integrated data above supersedes that exploratory result for this implementation.

GPU reconnection: eight sessions alternating 720p/1080p in one host passed. Observed private-memory growth was approximately 58 MiB, under the 96 MiB regression threshold; this does not establish leak-free prolonged use.

Discovery found generic graphics hosts in 32/64 bits. Unprepared modern processes remain explicitly disconnected. Black Desert Online was not tested.
