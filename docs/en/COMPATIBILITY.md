[Português](../COMPATIBILITY.md) · **English**

# Game compatibility

Status for **0.18.0**. Desktop output is the default and does not depend on the game's graphics API while Windows can compose a window above it.

| Component | Status |
| --- | --- |
| Windows | Windows 10 2004 or later / Windows 11, x64 |
| Edge and Chrome | Native PiP source, kept open and not minimized |
| Windowed and borderless | DirectComposition desktop output; no libraries loaded into the game |
| Exclusive fullscreen | Desktop output does not cover it; requires specific integration and validation |
| Direct D3D9 | Own host validated, including fullscreen |
| Direct D3D11 | Already-running x86/x64 hosts validated; exclusive fullscreen unvalidated |
| D3D12 / OpenGL | Prepared-launch integration validated in own x86/x64 hosts |
| Vulkan | Experimental: SDR RGBA/BGRA UNORM 8-bit, graphics presentation queue, one swapchain |
| Video | Up to 1920×1080; independent 15/30/60 fps, subject to source and hardware limits |
| Installation | Runtime and renderers included; no RTSS or extension; public signing pending |

## Recommended use

Open PiP Overlay and put the browser video in PiP. No process selection is needed: desktop output sits above windows and borderless games, passes clicks through and does not take focus. It never automatically switches to direct integration when a game is detected.

The user reported successful **desktop output over Black Desert Online on their home computer**, while selecting the process directly did not work. The exact presentation mode was not measured. This report does not certify direct integration, exclusive fullscreen or other game configurations.

## Optional direct connection

**Settings → More → Connect directly to game** enables window and graphics-module discovery. Ordinary applications may appear because they use graphics APIs too; detecting a module does not prove a process is a compatible game.

D3D9/11 can connect to running processes when permitted. D3D12, OpenGL and Vulkan may need **Launch configured game** from the tray menu. Choose the actual executable with **Change game**. Launchers, permissions and protections may prevent integration. There is no privilege elevation or protection bypass.

## Limits

- No promise of support for every game or zero performance cost.
- Real-game FPS impact was not measured for this release. Desktop mode skips game discovery but does not change video transport cost.
- HDR and AMD/NVIDIA GPUs were not validated by the project in this round. The home report is usage evidence, not a controlled benchmark.
- Vulkan does not compose with multiple swapchains, presentation extension chains, device groups or protected resources.
- Native PiP is still required; ordinary browser tabs are not extracted automatically. Protected video may not be capturable.

[Current validation](VALIDATION-0.18.md) · [0.17 renderer validation](VALIDATION-0.17.md) · [Test matrix](TEST-MATRIX.md) · [Architecture](ARCHITECTURE.md)
