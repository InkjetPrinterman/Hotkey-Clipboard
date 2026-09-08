# Hotkey Clipboard

A small Windows background utility that lets you build a library of
hotkey-triggered clipboard snippets. Press a hotkey, then a modifier+key
combo, to save whatever text is currently highlighted under that combo.
Press the same combo anywhere, anytime, to paste it back.

It runs entirely from the system tray — no application window is needed
for normal operation. A window only appears when you open Settings or
the shortcut manager.

## How it works

1. Press the **recording hotkey** (default `Ctrl+Alt+R`). A small
   on-screen notification appears and a countdown starts (default 5s).
2. While it's counting down, hold a modifier (Ctrl/Alt/Shift/Win) and
   press a key — e.g. `Ctrl+1`. Whatever text is currently highlighted
   anywhere on screen is copied and saved under that combo.
3. From then on, press `Ctrl+1` anywhere to paste that text.
4. Repeat with `Ctrl+2`, `Ctrl+Shift+A`, etc. to build up as many
   snippets as you want — each with its own paste combo.

If nothing was highlighted, or the countdown runs out before you press
a combo, nothing is saved and you'll see/hear a notification saying so.

While the recording window is open, **every key you press is swallowed
and never reaches whatever app has focus** — so pressing, say, "1" to
define `Ctrl+1` won't type "1" into Notepad (or anywhere else) and
overwrite your highlighted text. Press **Escape** at any point during
the recording window to cancel it early instead of waiting it out.

## Requirements to build

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio (optional — the `dotnet` CLI is enough)

## Build & run

From a command prompt in this folder:

```
dotnet build -c Release
dotnet run -c Release
```

Or open `HotkeyClipboard.sln` in Visual Studio and press F5.

## Publish a single portable .exe

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The .exe will be under `bin\Release\net8.0-windows\win-x64\publish\`.
Copy it anywhere on the target machine and double-click it. No installer
is included — you can add the app to your Windows startup folder, or
enable "Start automatically when Windows starts" from **Settings**
inside the app.

## Using the app

- **Tray icon** — right-click for the menu: Start Recording Now, Manage
  Saved Shortcuts, Settings, Exit.
- **Settings** — change the recording hotkey, the recording window
  length, whether to show a visual/sound notification while recording,
  whether to restore your clipboard automatically after each
  copy/paste, and whether to launch at sign-in.
- **Manage Saved Shortcuts** — see every combo you've recorded, a
  preview of the saved text, and delete ones you don't need anymore.



## Where your data is stored

Settings and snippets are saved locally, in plain text JSON, at:

```
%AppData%\HotkeyClipboard\settings.json
%AppData%\HotkeyClipboard\snippets.json
```

Nothing is sent anywhere — the whole app is offline. Because snippets
are stored as plain text, avoid saving anything you wouldn't want
readable by anything else with access to your user profile.

## Known limitations

- **Elevated/admin windows**: Windows blocks background apps from
  sending simulated keystrokes into windows running as Administrator
  unless the sender is *also* running as Administrator (this is a
  Windows security feature called UIPI, not a bug in this app). If you
  need to copy from or paste into an elevated app, run Hotkey
  Clipboard as Administrator too.
- **Copy relies on simulating Ctrl+C**: this is how most clipboard
  utilities of this kind work, but a handful of apps override or block
  simulated copy shortcuts. If a particular app doesn't cooperate,
  that's the likely reason.
- **Combo conflicts**: if the combo you pick during recording is
  already registered by another running app, Windows won't let this
  app also register it — you'll get a notification and can try a
  different combo.
- **One new snippet per recording window**: each time you trigger the
  recording hotkey, you define exactly one snippet. Trigger it again
  to add another.
- **All keys are blocked system-wide while recording**: this is by
  design (see above), but it does mean things like Alt+Tab won't work
  for the few seconds the recording window is open. It closes as soon
  as you press a combo (or Escape), so this window is intentionally
  short.

## Uninstalling

1. Open Settings and uncheck "Start automatically when Windows
   starts" (if enabled), or just delete the shortcut from your
   Startup folder if you added it manually.
2. Exit the app from the tray icon.
3. Delete the .exe and, if you want to remove your saved snippets too,
   delete the `%AppData%\HotkeyClipboard` folder.
