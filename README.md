# hdt-wine-overlay-hide

A [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker) plugin for running HDT under Wine or Proton on Linux. When Hearthstone loses focus, the plugin hides HDT's overlay, so the overlay no longer stays drawn on top of every other window.

## The problem

When Hearthstone goes to the background, HDT doesn't hide its overlay. It sends the overlay to the bottom of the window stack and leaves it drawn. That works on Windows.

Under Wine, the overlay is a no-activate, topmost, click-through window, so Wine creates it as an override-redirect X window. The compositor (KWin, Mutter, etc.) doesn't manage override-redirect windows and always draws them above normal windows. HDT's "send to back" therefore does nothing, and the overlay floats over your browser, terminal and everything else until you click back into Hearthstone.

Wine isn't at fault here. I logged `GetForegroundWindow()` while switching apps, and it moved to the desktop window about a second after Hearthstone lost focus. HDT sees the change and handles it in a way that only works on Windows, so no Wine setting fixes it.

## What the plugin does

About every 100 ms, the plugin checks whether Hearthstone is in the foreground. When HDT would push the overlay behind other windows, the plugin turns on HDT's own "Hide overlay" option instead. This sets the overlay to opacity 0 and makes it click-through. When Hearthstone gets focus back, the plugin turns the option off again.

It follows your existing "hide overlay in background" settings, both for matches and for menus. If you turned the overlay off yourself, the plugin leaves it off.

HDT writes `config.xml` in many places, including at shutdown before plugins unload, so the temporary hide can end up saved to disk. While the overlay is hidden, the plugin keeps a marker file in the Plugins folder. On the next start it clears the saved setting, so the overlay doesn't stay hidden for good.

## Install

1. Download `WineOverlayHide.dll` from the [latest release](../../releases/latest).
2. Copy it to `drive_c/users/<user>/AppData/Roaming/HearthstoneDeckTracker/Plugins/` in the Wine prefix where HDT runs. Under Proton the user is usually `steamuser`.
3. Restart HDT.
4. In HDT, go to Options → Tracker → Plugins and enable **Wine Overlay Hide**.

Built against HDT 1.60.3. If an HDT update breaks the plugin, HDT disables it and shows an error. Rebuild it from source against the new version.

## Build from source

`build.sh` runs the .NET Framework `csc.exe` that HDT's prefix already contains and compiles against the installed `HearthstoneDeckTracker.exe`. You don't need a .NET SDK on the Linux side.

Plain Wine:

```sh
WINEPREFIX=~/path/to/prefix ./build.sh --install
```

Proton through umu (for example a Lutris prefix):

```sh
WINEPREFIX=~/path/to/prefix \
PROTONPATH=/usr/share/steam/compatibilitytools.d/proton-cachyos-slr \
GAMEID=umu-battlenet \
./build.sh --install
```

Use the same `PROTONPATH` and `GAMEID` as the launcher that runs HDT. This lets the build join the running wineserver instead of starting a second one. `--install` copies the DLL into HDT's Plugins folder.

## Limitations

- The overlay also hides when you focus HDT's own main window, because Hearthstone isn't the foreground window then either.
- While the plugin holds the hide, the "Hide overlay" checkbox in HDT's options shows as checked.
- Tested on KDE Plasma (Wayland, XWayland) with proton-cachyos. Other compositors should behave the same, since every compositor stacks override-redirect windows on top.

## License

GPL-3.0-or-later. See [LICENSE](LICENSE).
