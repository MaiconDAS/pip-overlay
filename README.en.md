# PiP Overlay

[Português](README.md) · **English** · [Documentation](docs/en/README.md) · [Privacy](PRIVACY.md)

Watch your browser video while playing. Mouse and keyboard input stay with the game.

PiP Overlay brings an Edge or Chrome Picture-in-Picture window into supported game renderers on Windows. Position and resize the video in the app's visual editor. The in-game overlay does not receive clicks or keyboard focus. No RTSS, browser extension, separate runtime download or account is required.

[Download the installer](https://github.com/MaiconDAS/pip-overlay/releases) · [Report a problem](https://github.com/MaiconDAS/pip-overlay/issues)

## Version 0.16

- Automatic three-texture GPU sharing for **DirectX 11**, with CPU fallback.
- Exact placement against all four screen edges, even with margin snapping enabled.
- Subtle rounded video corners, with input interaction only in the editor.
- Privacy/security policies and principal documentation in Portuguese and English.

The transport optimization is specific to D3D11; other APIs retain their compatibility path. See [measured validation](docs/en/VALIDATION.md) for the evidence and limits, not a universal game-FPS promise.

![Editor with a settings sidebar](docs/assets/workspace-016.png)

## Getting started

1. Run `Instalar-PiP-Overlay.exe`. It creates a Start menu entry, an optional desktop shortcut, and a Windows uninstall entry. Updates preserve preferences.
2. Open PiP Overlay and put a video into the browser's native Picture-in-Picture mode. Keep that window open and not minimized.
3. For the newer graphics APIs, launch the selected game through **Iniciar jogo configurado** in the app's tray menu. Choose the actual game executable on first use. Supported sources connect automatically.
4. Drag the video in the editor and resize its edges or corners. Settings live in the **Imagem**, **Posição** and **Mais** sidebar sections. The application interface currently remains in Portuguese; this is an English documentation edition.

Resolution and FPS are independent: up to 1080p at 30 fps can preserve sharpness with fewer updates than 60 fps. Actual resolution and frame rate depend on the source, browser, renderer and hardware. Pausing also stops automatic reconnection. Configure shortcuts under **Ajustes > Mais**. Closing the window keeps the app in the tray; choose **Sair** to exit.

## Compatibility and limitations

Windows 10 version 2004 or newer and Windows 11, x64. Native PiP from Edge and Chrome. Legacy recognized D3D9 processes, plus DirectX 11/12, OpenGL and experimental Vulkan renderers. Modern renderers were validated in dedicated 32-bit and 64-bit test hosts. API support is not certification for every game. Protected video, game protections, secondary launchers and some graphics configurations may prevent operation. The app does not bypass protections.

D3D9 fullscreen has dedicated-host validation. Other APIs require game-specific fullscreen validation; synthetic windowed results are not a promise of real-game compatibility. Vulkan currently supports SDR 8-bit RGBA/BGRA UNORM, a graphics presentation queue and a single swapchain per presentation; HDR and several advanced presentation modes remain unsupported.

[Compatibility details](docs/en/COMPATIBILITY.md) · [Measured performance and validation](docs/en/VALIDATION.md)

## Privacy and installation

Frames are processed locally; the application does not record or upload your video. Update checks contact GitHub and can be disabled. Preferences and local renderer logs remain on your computer; uninstall preserves preferences. See the full [privacy policy](PRIVACY.md) for network requests, diagnostics and removal of local data.

The installer is per-user and includes .NET and renderers. It does not install drivers/services or configure automatic startup. Public code signing is pending. SHA-256 verifies download integrity but is not a publisher signature. See [signing](docs/en/SIGNING.md).

## Development and license

Original project code is licensed under [MIT](LICENSE). Third-party components retain their own licenses: [notices](docs/en/THIRD_PARTY_NOTICES.md). See [development](docs/en/DEVELOPMENT.md), [architecture](docs/en/ARCHITECTURE.md), [contributing](docs/en/CONTRIBUTING.md) and [security](SECURITY.md).

Performance changes are accepted based on integrated tests, not isolated benchmark promises. Test tools are excluded from production builds. Changes to Windows installation behavior are tested on disposable runners first.

### Game discovery (0.16)

The list inspects open windows and graphics modules without a game-name allowlist. Discovery is read-only, cached for five seconds, and skipped while a session is connected and the editor is hidden. Other graphics applications may appear; finding a renderer does not establish game compatibility.

DirectX 9 uses the existing per-process connection. Unprepared DirectX 11/12, OpenGL and Vulkan processes appear as **não conectado** (not connected). Select the actual game executable with **Trocar jogo** under **Ajustes → Mais**, then use the configured-game launch entry in the tray menu. Selecting an executable also includes its window when the renderer is unknown. Launchers spawning other processes and protected games may prevent integration. There is no protection bypass or privilege elevation.

Black Desert Online has not been tested. Discovery and acceptance of the overlay by the game are separate checks; it is not listed as compatible merely because discovery was broadened.
