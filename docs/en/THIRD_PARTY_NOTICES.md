# Third-party notices

[Português](../../THIRD_PARTY_NOTICES.md) · [English index](README.md)

Original PiP Overlay code is [MIT licensed](../../LICENSE). This does not replace dependency licenses. Browser/game/technology names identify compatibility and do not imply affiliation or endorsement.

- **MinHook 1.3.4**, Tsuda Kageyu and contributors: [original license](../../vendor/minhook/LICENSE.txt), source in `vendor/minhook`.
- **.NET / Windows Desktop Runtime**, Microsoft and contributors: packages include `LICENSE-DOTNET.txt`, `LICENSE-WINDOWSDESKTOP.txt` and `THIRD-PARTY-NOTICES-DOTNET.txt` from the official runtime packages.
- **ReShade 6.8.0**, crosire and contributors: engine and SDK at commit `18deaa52de0c425a78b329e9cb3c497281cd00ec`, [BSD 3-clause license](../../vendor/reshade/LICENSE.md). Official binaries are extracted from the Add-on package; its installer is not executed. The app includes `LICENSE-RESHADE.md`.
- **Vulkan-Headers 1.3.290**, Khronos Group and contributors: commit `b379292b2ab6df5771ba9870d53cf8b2c9295daf`; [license and notices](../../vendor/vulkan/LICENSE.md) and notices in the headers. Used to build the custom layer.
- **Inno Setup 7.1.0**, Jordan Russell and Martijn Laan: unmodified installer engine, distributed with `LICENSE-INNO-SETUP.txt`. [Source](https://github.com/jrsoftware/issrc).
- **Zig 0.14.1**: portable build tool from the official distribution, not shipped inside the application installer.
- **Microsoft Visual C++ Build Tools / Windows SDK**: downloaded and extracted locally for development with hashes pinned in `scripts/msvc-packages.json`. Microsoft terms apply. Build tools are not shipped; the add-on statically links the C++ runtime, so users do not need a separate Visual Studio installation.
- **Innounp**: development-only extraction tool, not distributed with the app.

Official sources: [MinHook 1.3.4](https://github.com/TsudaKageyu/minhook/tree/v1.3.4), [ReShade 6.8.0](https://github.com/crosire/reshade/tree/v6.8.0), [Vulkan-Headers 1.3.290](https://github.com/KhronosGroup/Vulkan-Headers/tree/v1.3.290), [Zig 0.14.1](https://ziglang.org/download/0.14.1/).
