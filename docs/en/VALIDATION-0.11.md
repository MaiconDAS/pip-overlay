# Validation 0.11

[Português](../VALIDATION-0.11.md) · [Documentation](README.md)

> Historical record. For the current implementation, see [0.16 validation](VALIDATION.md).

Local Windows tests on Intel UHD Graphics 770. Dedicated hosts do not constitute validation in third-party games.

| Check | Result |
| --- | --- |
| Connection, shortcuts and geometry | 47 checks passed |
| Quality and window restoration | 7 checks passed |
| Native capture at 1280 × 720 and 1920 × 1080 | Every pixel byte preserved |
| DirectX 11 x64 and x86 | 20 checks per architecture, 5 positions, no mismatches |
| DirectX 12 x64 and x86 | 20 checks per architecture, 5 positions, no mismatches |
| OpenGL x64 and x86 | 20 checks per architecture, 5 positions, no mismatches |
| Vulkan x64 and x86, custom layer | 509 and 504 checks respectively, 5 positions, no mismatches |

The capture pattern includes one-pixel details and gradients to detect unwanted filtering. Composition uses known colors and proportional coordinates checked after rendering. This does not measure YouTube stream quality or prove 60 fps of actual browser video.

Vulkan through ReShade failed on this GPU because it required shaderStorageImageReadWithoutFormat. The custom transfer/blit layer passed without that requirement. OpenGL was checked through framebuffer readback because the host's RGBX format is unsupported by the engine's screenshot capture.

This version has no controlled real-game performance comparison. HDR and the special cases listed in the architecture are not enabled. Logs containing personal paths are kept out of Git.

Continuous WGC → transport → D3D9 host capture published 52.4 frames/s over 12.56 seconds, transferring 1282 × 752 with 25 pixel checks and no mismatches. The test process used about 21.2% of one logical core, including the synthetic source and validation readbacks. This is not the isolated cost of the overlay in a game or a measurement of 1080p60 browser video.

Installer: 491 files extracted into an isolated folder and SHA-256 compared with the package; an existing destination was rejected. No test hosts, test commands or personal settings were included in production.
