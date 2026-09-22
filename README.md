# AreaSnap

> Select an area. Capture it. Save or copy.

A native Windows screenshot tool built with .NET 8 WinForms.
Part of the [Area Suite](https://github.com/smouj) — simple, native tools for everyday tasks.

## Features

- **Region capture** — Click and drag to select any rectangular area of the screen
- **Full screen capture** — Grab the entire primary display
- **Copy to clipboard** — Instantly paste into any app
- **Save to file** — PNG or JPG with configurable quality
- **System tray** — Minimize to tray, Ctrl+Shift+S global hotkey
- **Remembered settings** — Format, folder, and mode persist between sessions
- **Dark overlay** — Dim the screen during selection for precise cropping

## Requirements

- Windows 10 version 2004+ (build 19041) or later
- .NET 8 runtime

## Build

```bash
dotnet build -c Release
```

To publish a self-contained exe:

```bash
dotnet publish src/AreaSnap.App -c Release -r win-x64 --self-contained
```

## Usage

1. Select capture mode: Region (default) or Full Screen
2. Choose output format (PNG or JPG) and destination
3. Click **Capture** or press **Ctrl+Shift+S**
4. For region mode: click and drag to select the area
5. The screenshot is copied to clipboard and/or saved to file

## Architecture

```
AreaSnap.App      — WinForms UI (MainForm, AppSettings, hotkey)
AreaSnap.Core     — Domain models (CaptureSettings, CaptureResult, enums)
AreaSnap.Capture  — Screen capture engine (ScreenCapture, RegionSelectorForm)
```

## License

MIT