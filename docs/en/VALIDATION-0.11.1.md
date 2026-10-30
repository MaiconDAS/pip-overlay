# Validation 0.11.1

[Português](../VALIDATION-0.11.1.md) · [Documentation](README.md)

> Historical record. For the current implementation, see [0.16 validation](VALIDATION.md).

- Connection, shortcut and geometry tests passed.
- 19 checks covering resolution/FPS, migration, persistence and source restoration passed.
- All 12 combinations of four resolution limits and three frame rates keep the parameters independent.
- 1080p at 30 fps survives saving and loading preferences.
- Existing adaptive-resolution and FPS preferences are preserved.
- The interface was inspected with separate controls and no Open game button on the main screen.
- Native renderers were unchanged. The [0.11 graphics tests](VALIDATION-0.11.md) remain documented; they were not repeated or presented as new game tests.

The reported flicker at 60 fps did not receive a specific fix in this version. Choosing 30 fps no longer automatically reduces resolution. Actual capture dimensions remain limited by the browser and source.

Installer: 493 files extracted into an isolated folder and SHA-256 compared with the package; an existing destination was rejected. MIT license and third-party notices included.
