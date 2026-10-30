# Architecture

[Português](../ARCHITECTURE.md) · [English index](README.md)

Capture uses Windows Graphics Capture and D3D11. Frames stay local; there is no encoding, audio capture, recording or cloud stream. Processing follows frame arrival with periodic liveness maintenance and prioritizes recent frames.

## Image and transport

Resolution and FPS are independent. Capture limits are 1920×1080, 1280×720, 854×480 or an overlay-size-adaptive limit; cadence targets are 60/30/15 fps. Native-size BGRA frames can bypass resampling. The browser PiP is temporarily enlarged and restored on stop, subject to browser limits. Website quality does not guarantee the actual PiP pixel dimensions. Adaptive-size requests are quantized to reduce reallocations while dragging.

The compatibility transport uses three asynchronous GPU readback buffers, CPU shared memory and renderer upload. Higher resolutions can increase memory and bandwidth usage. It remains the path for D3D9, D3D12, OpenGL and Vulkan.

The D3D11 shared-GPU transport uses a separate versioned control mapping and a three-texture keyed-mutex ring. The producer renders RGBA into a free texture; the consumer copies the newest completed texture into the effect texture. Acquisition is nonblocking, old completed frames are retired and unsupported/failed sharing falls back to the CPU path. It avoids the per-frame CPU readback/upload round trip but still performs GPU copies; it is not zero-copy. A retained source enables fallback when the source is paused. CPU readback buffers are released when GPU transport activates. Diagnostic reports identify the active transport. No new configuration panel is needed.

## Rendering and input

D3D9 uses a per-process renderer discovered by window and graphics module without game-name filters. D3D11, D3D12 and OpenGL use ReShade 6.8.0 with a custom add-on; the effect draws only the video rectangle and no interactive overlay. ReShade input hooks and gamepad navigation are disabled; its internal overlay is blocked. Vulkan uses a custom transfer/blit layer with per-swapchain-image commands, upload resources, fences and semaphores. Composition waits on the original presentation semaphores, then presentation waits for composition. Synchronization resources are reused only when the swapchain image is available again. Vulkan does not wait for the entire GPU each frame; full waiting occurs on resource destruction.

The video has a small six-pixel corner radius. D3D9 uses rounded geometry, the ReShade APIs use a rounded mask, and Vulkan uses a bounded set of clipped blit regions. The visual editor provides all mouse interaction; game input is not consumed. Exact screen-edge positions are valid even with margin snapping enabled.

## Launch and lifecycle

The tray action launches the selected executable through an architecture-matched helper before its entry point. Libraries, configuration and caches live under the app's `data/games`, not inside the game's folder. Vulkan enablement is scoped to the child process environment. Runtime launch does not alter the registry, services, drivers or global environment.

Pausing makes renderers passive; libraries remain loaded until the game exits. Closing the UI keeps the app in the tray. PiP discovery runs every 1.5 seconds without an active session and skips work when paused and hidden. A failure does not trigger infinite connection retries. The installer separately manages per-user shortcuts and uninstall registration.

## Limits

Secondary launchers, protected games/video, driver differences and unsupported Vulkan presentation modes can prevent operation. Vulkan currently targets SDR 8-bit RGBA/BGRA, a graphics presentation queue and a single swapchain; HDR/device groups are unsupported; presentation extension chains pass through without composition. Select the actual renderer executable when a launcher starts another process. Shared D3D11 resources may not open across incompatible adapters, so the compatibility path remains necessary. API support does not guarantee every game or 60 fps. See [validation](VALIDATION.md).
