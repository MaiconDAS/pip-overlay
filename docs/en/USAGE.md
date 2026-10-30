# Installation and usage

[Português](../pt-BR/USAGE.md) · [English index](README.md)

## Install or move to another computer

Copy `PiP-Overlay-v0.16.1.exe` to the other computer and run it. The desktop shortcut is optional and selected by default. Start menu shortcuts and a Windows Installed apps / Programs and Features entry are created automatically. The runtime and renderers are included; no browser extension, RTSS or separate runtime download is needed.

Existing registered installations are detected automatically. Updating in the same folder preserves preferences. Choosing a different destination imports the registered installation's preferences without overwriting existing destination settings. For an old portable copy, select its existing folder. There is no manual import-path step or whole-disk search.

## Connect video and game

Open PiP Overlay, then open the video's native PiP window in Edge or Chrome. Keep it open and not minimized. For modern graphics APIs use **Iniciar jogo configurado** in the tray menu and select the game executable on first use. The app connects automatically when a supported source and renderer are available. An ordinary browser tab alone is not a supported video source.

The game list identifies windows and graphics modules without game-name filters. A process may appear as **não conectado** when unprepared or inaccessible. Use **Ajustes → Mais → Trocar jogo** to configure another executable. Detection does not establish compatibility; Black Desert Online has not been validated. See [compatibility](COMPATIBILITY.md).

## Position, size and quality

Drag the PiP to move it and use its edges/corners to resize while preserving aspect ratio. **Centralizar** centers it; **Desfazer** or Ctrl+Z in the editor restores the previous layout. Alt temporarily bypasses snapping. With multiple PiPs, **Vídeos** opens source thumbnails. Profiles store layout and quality per game. The sidebar groups image quality in **Imagem**, monitor/position/snapping in **Posição**, and shortcuts, sources, updates and diagnostics in **Mais**.

Choose resolution independently from 15/30/60 fps. Up to 1080p at 30 fps retains the requested resolution; 1080p selected on the video website does not guarantee a 1920×1080 PiP capture. The app temporarily enlarges the source window and restores it on stop. Hardware/browser limits still apply. Automatic FPS can lower cadence within the chosen bounds without lowering your selected resolution.

The current build lets the PiP touch each screen edge even when margin snapping is enabled, and uses a subtle six-pixel corner radius. D3D11 GPU sharing is selected automatically when compatible; other APIs retain the CPU transport. Privacy policy copies are included in the installer in both languages.

## Pause, exit and uninstall

**Pausar** also stops automatic reconnection; **Retomar** resumes it. Closing the main window leaves the app in the tray. Use **Sair** to exit. The app does not enable Windows startup automatically.

Uninstall from Windows Installed apps, the Start menu or the uninstall shortcut in the installation folder. Close the app and games launched through it first. Preferences and personal files remain; see [privacy](../../PRIVACY.md) to remove local data. See [compatibility](COMPATIBILITY.md) and [signing](SIGNING.md) for limitations.
