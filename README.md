<p align="center">
  <img src="docs/images/logo.png" width="96" alt="Blinders logo">
</p>

<h1 align="center">Blinders</h1>

<p align="center">
  <b>Dim everything except the window you're working in.</b><br>
  A tiny Windows tray app for focus — made for wandering eyes, ADHD and busy multi-monitor desks.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white" alt="Windows 10 | 11">
  <img src="https://img.shields.io/badge/version-1.0.0-FFB020" alt="version 1.0.0">
  <img src="https://img.shields.io/badge/languages-30-2ea44f" alt="30 languages">
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT license">
  <img src="https://img.shields.io/badge/size-~150%20KB-lightgrey" alt="about 150 KB">
</p>

<p align="center">
  <b>English</b> ·
  <a href="README.ko.md">한국어</a> ·
  <a href="README.ja.md">日本語</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.es.md">Español</a> ·
  <a href="README.de.md">Deutsch</a> ·
  <a href="README.fr.md">Français</a>
</p>

<p align="center">
  <img src="docs/images/settings-dark.png" width="49%" alt="Settings, dark theme">
  <img src="docs/images/settings-light.png" width="49%" alt="Settings, light theme">
</p>

## Why

Like blinders on a racehorse: the window you are using stays bright, everything else fades into the background. Other windows, other monitors, the chat that keeps blinking — still there, just quiet. One exe, no installer, no account, no telemetry.

## Features

- **Focus dimming** — everything but the active window is dimmed, on every monitor, mixed DPI included
- **Spotlight mode** — also dims the taskbar and always-on-top windows
- **Keep the active monitor bright** — only the other monitors are dimmed
- **Fullscreen aware** — a monitor with a game or a fullscreen video is left alone, the others keep dimming
- **Night schedule** — a different darkness during the hours you choose
- **Accent border** around the active window (optional), **tint** colors, **fade** transitions
- **Excluded apps** — no dimming while they are active; pick from running apps
- **Pause** for 10 minutes or an hour, **start with Windows**
- **Hotkeys** for everything, fully customizable
- **Light / dark settings window** that follows Windows, with a clickable monitor map
- **30 languages**, right-to-left layout for Arabic, Persian and Hebrew

## Download

1. Download `Blinders-x.y.z.zip` from [Releases](../../releases).
2. Unzip it anywhere and run `Blinders.exe`. The settings open on first run.
3. Windows 10/11 already include everything it needs (.NET Framework 4.8). One download works on 64-bit and 32-bit Windows, including ARM64 PCs.

> The exe is not code-signed yet, so SmartScreen may warn the first time: click **More info → Run anyway**.

## Usage

Blinders lives in the system tray (the `^` area next to the clock). **Left-click** the icon for settings, **right-click** for the quick menu. The icon shows the state: on, off or paused.

| Hotkey | Action |
|---|---|
| `Ctrl` + `Alt` + `Win` + `D` | Turn dimming on / off |
| `Ctrl` + `Alt` + `Win` + `↑` / `↓` | Darker / brighter |
| `Ctrl` + `Alt` + `Win` + `E` | Add / remove the current app from exclusions |
| `Ctrl` + `Alt` + `Win` + `Q` | Quit |

Hotkeys can be changed in **Settings → Hotkeys**. The defaults are chosen so they don't take over keys other apps use.

Starting `Blinders.exe` again just opens the settings. Starting a newer version replaces the old one cleanly.

**Settings** are stored in `%APPDATA%\Blinders\Blinders.ini`. **Portable mode:** put a `Blinders.ini` (even an empty one) next to `Blinders.exe` and that file is used instead.

## Languages

English · 简体中文 · 繁體中文 · हिन्दी · Español · Français · العربية · বাংলা · Português · Русский · 日本語 · Deutsch · 한국어 · Türkçe · Tiếng Việt · Italiano · ไทย · Bahasa Indonesia · Polski · Українська · Nederlands · فارسی · עברית · Svenska · Čeština · Română · Ελληνικά · Magyar · Dansk · Suomi

The system language is used automatically (Settings → More → Language to change it). Most translations are first drafts — **corrections are very welcome.**

**Fix or add a language, no build needed:** Settings → More → *Export language template*, translate `template.ini`, save it as `lang\<code>.ini` (e.g. `pt-BR.ini`) and restart Blinders. Missing lines fall back to English. To ship it for everyone, open a pull request adding the file to [`lang/`](lang).

## Build from source

No SDK required — Windows ships the C# compiler.

```bat
build.bat      :: bin\Blinders.exe
release.bat    :: dist\Blinders-<version>.zip
```

Every `lang\*.ini` is embedded into the exe at build time.

## License

[MIT](LICENSE) © 2026 Joonit · [Changelog](CHANGELOG.md)
