# 0.18.0 validation

[Português](../VALIDATION-0.18.md) · **English** · [Documentation](README.md)

Date: November 5, 2026. Scope: desktop output by default, UX, monitor layouts, recovery and verified updates. Native renderers were not changed in this release.

## Local results

- Managed build and connection, shortcut, geometry, improvement, recovery and localization suites passed. Recovery has 30 checks: invalid/oversized archives, profile-key path traversal, backups, restoration after a write failure, desktop migration, explicit preferences, landscape/portrait proportions, keyboard editing, download integrity, cancellation and output-mode controls.
- All new texts have ten translations; live language changes preserve resolution. Screenshots cover all languages; Portuguese and English received visual review. This is not native-speaker linguistic review.
- Editor height stayed at 490 px across sidebar tabs; minimum-size test passed without horizontal scrolling. FPS selection and automatic-FPS toggle passed. Thumbnail, source selection, edge and undo tests passed.
- Desktop output in a synthetic host: expected pixel, shared GPU transport, 58 published and 57 consumer-uploaded frames in the initial sample, clicks passed to the underlying window, no overlay activation, topmost restoration after another window was activated, continued capture after resizing.
- Synthetic HTTP tests: correct payload accepted; wrong hash rejected without replacing a verified file; cancellation removes partial files; foreign URLs and prereleases rejected. No installer was executed on the user's Windows installation.

## Validation limits

Resolution/orientation math was tested without changing Windows display configuration. Physical unplug/replug and mixed-DPI monitors still need suitable hardware tests. No controlled game benchmark, new exclusive-fullscreen test or HDR test was performed in this round.

The static-source regression test observed 17 wakeups and no newly processed frames in five seconds; this is not evidence of percentage performance gains in games. Desktop mode skips game module queries, but capture/composition still consumes CPU/GPU resources. No promise of zero cost or universal 60 fps.

The user reported desktop output working over Black Desert Online at home while direct process selection failed. The presentation mode and machine metrics were not confirmed. This does not certify direct integration with the game.

Public signing remains pending. SHA-256 checks correspondence with GitHub's reported file, not publisher identity. Preference archives use path-hashed game profile keys; different paths or monitor identifiers on another PC require reassociation.

## Package and installation

Production excludes tests and synthetic sources. Local validation extracts the installer and compares its files with the portable package. Installation, upgrade, shortcuts and uninstall are checked only through the disposable GitHub Windows workflow before publication. Run results are associated with the release.

[Compatibility](COMPATIBILITY.md) · [0.17 renderer tests and measurements](VALIDATION-0.17.md)
