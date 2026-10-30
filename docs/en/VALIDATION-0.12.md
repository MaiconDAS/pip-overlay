# Validation 0.12 — stability and performance

[Português](../VALIDATION-0.12.md) · [Documentation](README.md)

> Historical record. For the current implementation, see [0.16 validation](VALIDATION.md).

Available environment: Intel UHD Graphics 770, driver 32.0.101.7092, Windows x64. Tests use project-owned applications; they do not certify games.

## Contention flicker

The test holds the transport lock for 1.8 seconds while the producer remains alive. Version 0.11.1 drew 0 frames during that interval. The corrected renderer drew 103 frames, with 4 successful pixel checks and no mismatches. It reuses the previous image until the next is available. This fixes the reproduced case, without claiming that every possible flicker cause is eliminated.

## Memory and transport

Native-size BGRA capture skips two intermediate textures. At 1920 × 1080 this avoids approximately 15.8 MiB of image allocations, excluding driver overhead. One-pixel patterns remain exactly preserved at 720p/1080p.

The `native/shared_gpu_probe.cpp` prototype transferred a 1080p texture between two D3D11 processes using a keyed mutex and checked every pixel. Its 240-iteration microtest measured 18.48 ms per iteration for sharing and 13.09 ms for the synchronous CPU path. These are synthetic operations with different synchronization, not application FPS measurements or a definitive comparison. Evidence was insufficient to enable this transport in the distributed build on this machine; it remained a reproducible experiment outside the installer.

Technical reference: [Microsoft OpenSharedResource](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11device-opensharedresource).

## Automatic FPS

Optional and off by default. It uses capture processing time, not the video's natural cadence, to reduce FPS after sustained overload, then recovers gradually after stability. It respects the chosen minimum and configured maximum and never changes resolution. It does not directly measure game FPS.

## Profiles, updates and privacy

Profiles identified by an executable-path hash store position, size, monitor, resolution, FPS and automatic mode. Global shortcuts remain independent. Update checks query only this repository's releases and can be disabled. Files are not installed or executed automatically. Installer import copies only preference/profile JSON and preserves the previous installation.

Diagnostics include version, OS, architecture, capture limits and technical counters. They exclude titles, URLs, images, executable paths, shortcuts and exception text. Export goes only to the user-selected destination; there is no automatic upload.

## Rendering comparison

Sequential smoke test with a synthetic source and windowed D3D9 host, targeting 30 PiP fps. The baseline uses the 0.11.1 renderer with the current capture/harness to isolate the renderer change.

| Phase | Host FPS | Frame-time p95 |
| --- | ---: | ---: |
| Previous renderer, overlay active | 59.99 | 17.96 ms |
| Corrected renderer, no overlay | 59.90 | 17.64 ms |
| Corrected renderer, overlay active | 59.93 | 18.40 ms |
| Corrected renderer, final no-overlay phase | 59.91 | 29.99 ms |

PiP published 29.56 fps during the corrected phase. No host-state restoration errors were found. The changing p95 even without an overlay indicates scheduling/load noise; these numbers do not demonstrate an FPS gain or measure real-game impact. Complete data, including CPU: [benchmark](../benchmarks/0.12-intel-uhd770.json).

In the final run, D3D9 maintained 107 frames and four pixel checks during contention. D3D11/D3D12/OpenGL passed on x86/x64 with 20 checks per API/architecture. Vulkan passed 511 x64 and 510 x86 checks without mismatches, continuing to draw 72 frames during contention on each architecture.

Installer: 493 SHA-256 verified files, without internal tools or personal data. Preference/profile import was tested in isolated folders: source preserved, unrelated files excluded, invalid import rolled back without creating the final installation.
