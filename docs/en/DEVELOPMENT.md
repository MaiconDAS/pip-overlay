# Development

[Português](../DEVELOPMENT.md) · [English index](README.md)

## Requirements and local builds

Windows 10 2004+ x64, .NET SDK 8 (`dotnet`), PowerShell 7, access to official NuGet on first restore, and portable Zig 0.14.1. Scripts keep tools and caches inside the project; they do not globally install dependencies or modify Windows registry, drivers, services or startup.

```powershell
pwsh ./scripts/Initialize-Tools.ps1
pwsh ./scripts/Build.ps1
pwsh ./scripts/Build-Native.ps1
```

Default output: `artifacts/app`. Pass `-ZigPath C:\tools\zig.exe` to use an existing compiler. NuGet uses `.cache/packages` unless `NUGET_PACKAGES` is set for the process. Zig downloads are SHA-256 checked before extraction. MinHook is vendored. Native modern renderers use pinned ReShade 6.8.0 and Vulkan headers plus locally extracted Microsoft build tools; Microsoft terms still apply. Initial downloads can be hundreds of megabytes.

## Tests

```powershell
pwsh ./scripts/Test.ps1
pwsh ./scripts/Test-Graphics.ps1 -ZigPath C:\tools\zig.exe
pwsh ./scripts/Test-UX.ps1
```

Managed tests cover connection state, shortcuts, geometry, settings and diagnostics without registering global hotkeys or attaching to a user's game. Graphics tests open dedicated x86/x64 D3D11, D3D12, OpenGL and Vulkan hosts, test positions/pixels and exercise continuous D3D9 capture. UX tests need an interactive Windows desktop with DWM. Graphics support depends on installed drivers.

Development-only commands include `--gpu-test report.json`, `--gpu-test report.json --fullscreen`, `--soak-test report.json`, `--workspace-test report.json` and `--shared-test report.json pixels 64` (also `32`). Shared transport benchmarks accept `cpu` or `gpu` instead of `pixels`. Run from a development build with the required native hosts, graphics libraries and effects alongside it. Fullscreen tests temporarily occupy the display. All these modes and their test classes are excluded from production builds.

`Test-Performance.ps1` runs the configuration matrix and experimental alternatives; `Test-SharedGpu.ps1` is an isolated transport probe. Separate source-generation CPU from capture cost and distinguish requested, captured, published and uploaded FPS. Do not benchmark competing variants simultaneously or claim an isolated microbenchmark result as a game-FPS improvement.

## Packaging and installer validation

```powershell
pwsh ./scripts/Package.ps1 -ZigPath C:\tools\zig.exe
pwsh ./scripts/Test-Package.ps1
```

Outputs include `dist/installer/PiP-Overlay v0.16.1.exe`, `dist/SHA256SUMS.txt` and a portable ZIP. Production packages exclude test hosts, development commands, personal settings and machine-specific catalogs. Inno Setup 7.1.0 is extracted locally after hash verification; its installer is not run on the development machine.

`Test-Package.ps1` extracts and compares files without installing. `Test-InstallerWizard.ps1` and `Test-InstallerIntegration.ps1` only run on disposable GitHub-hosted Windows runners. They check the wizard, installation, shortcuts, uninstall registration, update/relocation and data preservation. Never use the user's Windows installation as the first test. Public signing is [pending](SIGNING.md).

## Layout and data

`src` contains WinForms code and development tests; `native` contains capture, transport, renderers and helpers; `effects` holds the ReShade effect; `installer` holds Inno setup; `scripts` covers builds and verification; `vendor` preserves dependency source/licenses. `Rtss*` and old V4 classes are historical development references excluded from production.

Profiles live in `data/profiles`, identified by a hash of the executable path. Local game launch data lives in `data/games`. Update checks can be disabled; the app opens release pages instead of silently running an installer. Do not commit binaries, tokens, personal logs or `PipD3D9-*.bin` catalogs. Keep both documentation language editions current.


## Additional reproducible checks

With the development build and native files in artifacts/tests, run --contention-test report.json or --gpu-benchmark report.json for D3D9 contention and no-overlay/overlay/no-overlay FPS, frame-time p95 and CPU measurements. Test-Upgrade.ps1 exercises preference import only in temporary artifacts folders. The --soak-test mode runs 30 capture cycles using PipTest32.exe; --shared-test report.json reconnect 64 runs eight integrated-GPU session recreations. These development modes are excluded from distribution.

Render-AppIcon.ps1 reproduces the icon; versioned final assets do not need regeneration for normal builds. MSVC is needed for ReShade SDK ABI compatibility; Zig builds the other native components. Native downloads are hash-verified and kept locally. Test-Package.ps1 also rejects an existing extraction destination and leaves the isolated test directory available for inspection.

Update checks run at most once per successful day on application startup and do not transmit preferences. They open the release page for the user to download the installer. Executable-path hashes identify profiles; global shortcuts remain separate.

Run scripts/Test-Documentation.ps1 to verify local links, language pairs and version alignment. scripts/Get-InstallerName.ps1 derives the installer filename from the project version. Update both documentation editions and the version manifests for each release.
