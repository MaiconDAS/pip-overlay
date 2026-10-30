# Compatibility

[Português](../COMPATIBILITY.md) · [English index](README.md)

| Component | Support and limits |
| --- | --- |
| Windows | Windows 10 2004 or later, Windows 11; x64 OS |
| Browser source | Native PiP from Edge or Chrome; keep it open and not minimized |
| DirectX 9 | Legacy recognized processes; not generic detection for every D3D9 game |
| DirectX 11 / 12 | Included engine and custom add-on; pixel tests in dedicated x86/x64 hosts |
| OpenGL | Included engine and custom add-on; pixel tests in dedicated x86/x64 hosts |
| Vulkan | Custom experimental layer; SDR 8-bit RGBA/BGRA UNORM and graphics queue presentation |
| Fullscreen | In-process composition; D3D9 dedicated-host validation. Newer APIs require game-specific validation |
| Windowed / borderless | Windowed synthetic tests; real games still require individual validation |
| Resolution / FPS | Up to 1920×1080; targets of 15/30/60 fps, not guarantees |
| Installation | One per-user installer; bundled runtime/renderers; no RTSS or extension |

For modern APIs, launch the game through the app's tray menu. Launchers that spawn another executable, game protections and protected video may prevent capture or connection. No bypass is implemented. There is no certified list of games for the modern APIs and no general guarantee of reduced game-FPS impact.

Vulkan does not support HDR, multiple swapchains in one presentation, presentation extension chains, device groups or protected resources in the current composition path. Unsupported cases pass through without composition. The browser PiP window remains necessary; ordinary tabs are not extracted directly.

GPU transport support and rendering API support are separate. A transport optimization for D3D11 does not automatically accelerate D3D9, D3D12, OpenGL or Vulkan. See [architecture](ARCHITECTURE.md) and [validation](VALIDATION.md).

### Game discovery (0.16)

The list inspects open windows and graphics modules without a game-name allowlist. Discovery is read-only, cached for five seconds, and skipped while a session is connected and the editor is hidden. Other graphics applications may appear; finding a renderer does not establish game compatibility.

DirectX 9 uses the existing per-process connection. Unprepared DirectX 11/12, OpenGL and Vulkan processes appear as **não conectado** (not connected). Select the actual game executable with **Trocar jogo** under **Ajustes → Mais**, then use the configured-game launch entry in the tray menu. Selecting an executable also includes its window when the renderer is unknown. Launchers spawning other processes and protected games may prevent integration. There is no protection bypass or privilege elevation.

Black Desert Online has not been tested. Discovery and acceptance of the overlay by the game are separate checks; it is not listed as compatible merely because discovery was broadened.
