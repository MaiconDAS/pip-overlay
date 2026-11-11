# Architecture

[Português](../ARCHITECTURE.md) · [English index](README.md)

Capture uses Windows Graphics Capture and D3D11. Frames stay local; there is no encoding, audio capture, recording or cloud stream. Processing follows frame arrival with periodic liveness maintenance and prioritizes recent frames.

## Image and transport

Resolution and FPS are independent. Capture limits are 1920×1080, 1280×720, 854×480 or an overlay-size-adaptive limit; cadence targets are 60/30/15 fps. Native-size BGRA frames can bypass resampling. The browser PiP is temporarily enlarged and restored on stop, subject to browser limits. Website quality does not guarantee the actual PiP pixel dimensions. Adaptive-size requests are quantized to reduce reallocations while dragging.

The compatibility transport uses three asynchronous GPU readback buffers, CPU shared memory and renderer upload. Higher resolutions can increase memory and bandwidth usage. It remains the path for D3D9, D3D12, OpenGL and Vulkan.

The D3D11 shared-GPU transport uses a separate versioned control mapping and a three-texture keyed-mutex ring. The producer renders RGBA into a free texture; the consumer copies the newest completed texture into the effect texture. Acquisition is nonblocking, old completed frames are retired and unsupported/failed sharing falls back to the CPU path. It avoids the per-frame CPU readback/upload round trip but still performs GPU copies; it is not zero-copy. A retained source enables fallback when the source is paused. CPU readback buffers are released when GPU transport activates. Diagnostic reports identify the active transport. No new configuration panel is needed.

## Rendering and input

D3D9 uses a per-process renderer discovered by window and graphics module without game-name filters. D3D11 can attach to an existing process using Present/Present1 hooks and isolated context state. D3D11 prepared-launch compatibility, D3D12 and OpenGL use ReShade 6.8.0 with a custom add-on; the effect draws only the video rectangle and no interactive overlay. ReShade input hooks and gamepad navigation are disabled; its internal overlay is blocked. Vulkan uses a custom transfer/blit layer with per-swapchain-image commands, upload resources, fences and semaphores. Composition waits on the original presentation semaphores, then presentation waits for composition. Synchronization resources are reused only when the swapchain image is available again. Vulkan does not wait for the entire GPU each frame; full waiting occurs on resource destruction.

The video has a small six-pixel corner radius. D3D9 uses rounded geometry, the ReShade APIs use a rounded mask, and Vulkan uses a bounded set of clipped blit regions. The visual editor provides all mouse interaction; game input is not consumed. Exact screen-edge positions are valid even with margin snapping enabled.

## Launch and lifecycle

The tray action launches the selected executable through an architecture-matched helper before its entry point. Libraries, configuration and caches live under the app's `data/games`, not inside the game's folder. Vulkan enablement is scoped to the child process environment. Runtime launch does not alter the registry, services, drivers or global environment.

Pausing makes renderers passive; libraries remain loaded until the game exits. Closing the UI keeps the app in the tray. Out-of-context WinEvent notifications trigger debounced discovery, with periodic fallback checks every 5 seconds when visible or 15 seconds when hidden. Paused hidden discovery skips work. Transient capture failures allow at most two delayed retries; process connection failures can fall back to independent desktop output without repeatedly attaching to the failed process. The installer separately manages per-user shortcuts and uninstall registration.

## Limits

Secondary launchers, protected games/video, driver differences and unsupported Vulkan presentation modes can prevent operation. Vulkan currently targets SDR 8-bit RGBA/BGRA, a graphics presentation queue and a single swapchain; HDR/device groups are unsupported; presentation extension chains pass through without composition. Select the actual renderer executable when a launcher starts another process. Shared D3D11 resources may not open across incompatible adapters, so the compatibility path remains necessary. API support does not guarantee every game or 60 fps. See [validation](VALIDATION.md).

## Independent output and languages

The desktop backend uses D3D11 shared textures and a DirectComposition surface. It presents only newly captured frames. Its layered, transparent, non-activating window passes hit testing through; topmost placement explicitly uses NOACTIVATE. This path supports windowed/borderless, not exclusive fullscreen.

Ten embedded language catalogs share stable Portuguese source keys. The global language setting is validated and persisted separately from game profiles. Updating the UI language does not dispose or recreate the capture session.

## Desktop-first lifecycle and recovery (0.18)

Desktop output is the default, including a one-time migration of older settings. In this mode, target enumeration returns only the desktop backend: game module discovery and automatic promotion to a game process are skipped. Browser discovery retains its event-driven and periodic fallback behavior. Direct mode must be selected explicitly.

Monitor layouts store reference resolution and rectangle. Display changes stop the current session, rebuild the monitor list, fit the saved rectangle and reconnect. Import uses a bounded JSON archive, hashed profile keys, a pre-import backup and restoration of changed files if writing fails. Installer downloads use bounded streaming SHA-256 verification against official GitHub asset metadata; partial files are discarded. The app never executes the downloaded installer automatically. Diagnostic rates are session averages, not game FPS measurements.

## Desktop scheduling in 0.19

The producer signals a local new-frame event. The desktop consumer coalesces notifications on its UI thread and retains a one-second maintenance check. Busy presentation requests are retried; accepted submissions are counted separately from attempts. Backbuffer views and rendering resources are reused during resizing. Transparent output is cleared before drawing. Paused/hidden discovery is stopped. Lock/sleep notifications stop capture and trigger recovery when appropriate; transient device failures have bounded retries. These mechanisms are implemented; their remaining validation limits are recorded in [0.19 evidence](VALIDATION-0.19.md).
