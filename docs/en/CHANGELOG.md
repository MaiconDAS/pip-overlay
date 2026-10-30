# Changelog

[Português](../../CHANGELOG.md) · [English index](README.md)

## 0.16 — shared GPU transport and documentation

- Automatic D3D11 shared-GPU texture ring with CPU fallback; other APIs retain their compatibility transport.
- Exact screen-edge placement despite margin snapping, and subtle rounded video corners.
- Privacy/security policies and Portuguese/English documentation for use, compatibility, development, architecture, contributing, signing and dependencies.
- Integrated transport measurements and pixel/resize/fallback checks; no general claim of improved FPS in every game.

- Window/renderer-based discovery without game-name filters; unprepared processes are explicitly shown as disconnected.

## 0.15

Settings moved into an Imagem / Posição / Mais sidebar without reducing editor height. Modern selectors, buttons, toggles and margin slider. Frame-arrival-driven capture, recent-frame priority, static-image maintenance, reduced paused/hidden discovery and diagnostic counters. Synthetic measurements were published without claiming proven game-FPS gains.

## 0.14.1

Removed the manual import-path step. The installer detects registered installations, updates existing folders without an extra existing-folder warning, preserves local preferences and imports registered preferences on relocation without overwriting destination data. Added interactive wizard validation.

## 0.14

Complete per-user Inno installer with optional desktop shortcut, Start menu entries, Windows uninstall registration and preservation of user data. New multi-resolution application icon. No public code signing yet.

## 0.13

Visual source selection, snapping/margins, centering/undo, clearer connection states and game profiles. Prepared a signing workflow without claiming public signing.

## 0.12

Quality/FPS controls and reliability improvements, local profiles, diagnostics and optional update checks. Resolution and FPS remain independent.

## 0.11 series

Added modern graphics renderers alongside the legacy D3D9 path, with synthetic pixel validation and documented API limitations. See [validation](VALIDATION.md) and the [full historical Portuguese log](../../CHANGELOG.md).
