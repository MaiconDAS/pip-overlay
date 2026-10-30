# Performance investigation — October 29, 2026

[Português](../PERFORMANCE-2026-10-29.md) · [Documentation](README.md)

> Exploratory investigation preceding 0.16 integration. Current integrated results: [validation](VALIDATION.md).

## Environment and scope

Intel UHD Graphics 770, driver 32.0.101.7092. Local tests used an animated 1920×1080 WinForms source and a dedicated windowed D3D9 renderer. No real game, browser, installation or global Windows adjustment was used. Producer CPU includes the synthetic source; percentages refer to one core, not the whole computer.

## Configuration matrix

Three repetitions per configuration in alternating order, each with approximately six seconds without overlay, six with overlay, then six without again. Medians:

| Configuration | Published frames/s | Sampled submission cost | Additional renderer CPU, percentage points of one core |
| --- | ---: | ---: | ---: |
| 1080p / 30 fps | 28.76 | 2.86 ms | 14.23 |
| 1080p / 60 fps | 34.37 | 2.09 ms | 15.41 |
| 720p / 60 fps | 40.99 | 1.30 ms | 10.48 |

The renderer maintained approximately 60 presentations/s in this matrix. This does not represent a demanding game's performance. Submission cost is the mean of samples of the latest submission, including readback, not end-to-end latency. Cost per submission does not measure total cost per second.

720p reduces observed cost but changes resolution; it is not a quality-preserving optimization. Requesting Full HD at 60 fps did not guarantee 60 published frames. Additional instrumentation confirmed a source near 60 fps in some runs with much lower output, justifying investigation of transport and synchronization.

## Alternatives without reducing resolution

Final A/B tests ran in this order: current, bulk copy, unpaced, unpaced, bulk copy, current. The test executable waits for its renderer to exit before the next run. An earlier exploratory round failed while replacing a DLL still in use and was discarded completely; it is excluded from the published data.

| Run | Source /s | Published /s | Renderer frame-time p95 |
| --- | ---: | ---: | ---: |
| Current 1 | 60.4 | 30.9 | 32.83 ms |
| Bulk copy 1 | 39.3 | 19.1 | 72.35 ms |
| Unpaced 1 | 27.0 | 14.4 | 66.70 ms |
| Unpaced 2 | 43.9 | 24.7 | 42.87 ms |
| Bulk copy 2 | 63.4 | 36.4 | 18.87 ms |
| Current 2 | 52.8 | 25.3 | 33.30 ms |

Source and no-overlay phase variation indicate unstable conditions. These results cannot attribute improvement or regression solely to a variant. Bulk copy and removal of pacing must not be promoted as proven optimizations. Neither variant was applied to the distributed build. Unpaced mode is a diagnostic option excluded from production.

## GPU shared texture

Three additional repetitions of 240 Full HD transfers, performed sequentially after the application test:

| Repetition | GPU sharing | Synchronous readback + upload |
| --- | ---: | ---: |
| 1 | 0.8350 ms | 3.0581 ms |
| 2 | 0.7643 ms | 3.3062 ms |
| 3 | 0.8149 ms | 3.4633 ms |

The median was 0.8149 ms versus 3.3062 ms, about 75% less time **in this microbenchmark**. Cross-process pixel validation passed all three repetitions. The timed loop uses two D3D11 devices in one process; a separate cross-process test checks pixels. The CPU reference is synchronous and does not reproduce 0.15's asynchronous transport or integrate the application's D3D9 renderer.

This confirms feasibility and justifies an integrated prototype, initially for D3D11. It does not demonstrate a 75% reduction in application cost or equivalent compatibility for D3D9/12, OpenGL or Vulkan. An initial exploratory pass was much slower (10.66/20.77 ms), reinforcing the need for warmup, repetition and load control.

## Conclusion and next experiment

Direct shared-GPU-texture transport is promising. The next comparison should use the same renderer, resolution, source and cadence, with warmup and repeated runs, measuring CPU, frame time and pixel stability. Keep fallback until benefit is established. The configuration matrix also supports 1080p/30 as a sharpness option with more predictable cadence on this machine.

[Raw data](../benchmarks/performance-2026-10-29). Reproduce the matrix and variants with `scripts/Test-Performance.ps1`, passing an existing Zig compiler and native test-binary directory. The separate sharing test is `scripts/Test-SharedGpu.ps1`. No new installer was produced for this investigation.
