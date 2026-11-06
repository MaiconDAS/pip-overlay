# Installation and usage

[Português](../pt-BR/USAGE.md) · [English index](README.md)

## Install or move to another computer

Copy `PiP-Overlay-v0.18.0.exe` to the other computer and run it. The desktop shortcut is optional and selected by default. Start menu shortcuts and a Windows Installed apps / Programs and Features entry are created automatically. The runtime and renderers are included; no browser extension, RTSS or separate runtime download is needed.

Existing registered installations are detected automatically. Updating in the same folder preserves preferences. Choosing a different destination imports the registered installation's preferences without overwriting existing destination settings. For an old portable copy, select its existing folder. There is no manual import-path step or whole-disk search.

## Open PiP and play

Open PiP Overlay and the video's native PiP in Edge or Chrome. Keep the source open and not minimized. The default desktop overlay sits above windows and borderless games, passes clicks through and does not take keyboard focus. You do not need to select a game or restart it. An ordinary browser tab alone is not a supported source.

**Settings → More → Connect directly to game** reveals advanced game selection. This mode can load integration into a compatible process; it is not needed for desktop output. D3D9/11 can connect to already-running processes when permitted; other APIs can require prepared launch from the tray menu. A listed process is not proof of compatibility. Desktop output does not cover exclusive fullscreen. See [compatibility](COMPATIBILITY.md).

## Language

Choose one of ten languages under **Settings → More → Language**. Changes apply without restarting capture and are saved globally, separately from game profiles. Technical Windows messages may follow the system language.

## Position, size and quality

Drag the PiP to move it and use its edges/corners to resize while preserving aspect ratio. **Center** centers it; **Undo** or Ctrl+Z in the editor restores the previous layout. Alt temporarily bypasses snapping. With multiple PiPs, **Videos** opens source thumbnails. Profiles store layout and quality per game. The sidebar groups image quality in **Image**, monitor/position/snapping in **Position**, and shortcuts, sources, updates and diagnostics in **More**.

Choose resolution independently from 15/30/60 fps. Up to 1080p at 30 fps retains the requested resolution; 1080p selected on the video website does not guarantee a 1920×1080 PiP capture. The app temporarily enlarges the source window and restores it on stop. Hardware/browser limits still apply. Automatic FPS can lower cadence within the chosen bounds without lowering your selected resolution.

The current build lets the PiP touch each screen edge even when margin snapping is enabled, and uses a subtle six-pixel corner radius. D3D11 GPU sharing is selected automatically when compatible; other APIs retain the CPU transport. Privacy policy copies are included in the installer in both languages.

## Pause, exit and uninstall

**Pause** also stops automatic reconnection; **Retomar** resumes it. Closing the main window leaves the app in the tray. Use **Exit** to exit. The app does not enable Windows startup automatically.

Uninstall from Windows Installed apps, the Start menu or the uninstall shortcut in the installation folder. Close the app and games launched through it first. Preferences and personal files remain; see [privacy](../../PRIVACY.md) to remove local data. See [compatibility](COMPATIBILITY.md) and [signing](SIGNING.md) for limitations.

## Layouts and keyboard

Arrow keys move the PiP by one physical pixel; Shift increases the step to ten. Ctrl+arrows resize while preserving aspect ratio. Ctrl+Z undoes the last gesture. These keys work only when the editor has focus. The overlay remains noninteractive.

Monitor layouts are saved separately within the current profile. Resolution changes scale the size proportionally and preserve the relative position, including screen edges. If the preferred monitor disconnects, the app falls back to the primary display without forgetting the preferred display. Reconnecting restores its layout. Device names identify displays; Windows may assign different names on another PC. Game profiles apply in direct mode; desktop mode uses the current layout without scanning games.

## Preferences and recovery

Under **Settings → More**, export a JSON file and import it on another PC. Import validates the file, merges its game profiles, backs up existing preferences under `data/backups`, preserves the local executable path and pauses PiP. The app closes; reopen it to apply the settings, then choose Resume. Backups can be imported through the same action. Game profile keys depend on the executable path; different paths on another PC require selecting/copying a profile again. Monitor names may also need reselection.

**Updates** lists stable installers with SHA-256 metadata from the official GitHub release. It verifies size and hash before accepting the file, backs up preferences and allows selecting an older stable version. Close the app and run the downloaded installer yourself. Older versions may not understand new preferences; backups remain available. Hash validation is not a publisher signature; public code signing is pending. Closing the download window cancels the operation. No installer runs automatically. Downloads and backups are retained locally until removed by the user.

## 0.19 candidate

The main editor identifies the output mode. When several sources are available at startup while the editor is visible, it offers a video choice; cancellation pauses automatic connection. Use **Settings → More → Profiles and monitors** to apply an imported profile, assign a copy to a local game executable, or map a saved monitor layout to a current display. Legacy unnamed profiles show a short identifier. Monitor mapping preserves proportional placement and the old display entry. Actual lock/sleep and long-session recovery have not been validated in this candidate.
