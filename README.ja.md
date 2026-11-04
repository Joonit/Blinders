<p align="center">
  <img src="docs/images/logo.png" width="96" alt="Blinders ロゴ">
</p>

<h1 align="center">Blinders</h1>

<p align="center">
  <b>作業中のウィンドウ以外を、すべて暗く。</b><br>
  集中のための小さな Windows トレイアプリ — 視線が散りがちな人、ADHD、マルチモニター環境のために。
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white" alt="Windows 10 | 11">
  <img src="https://img.shields.io/badge/version-1.0.0-FFB020" alt="version 1.0.0">
  <img src="https://img.shields.io/badge/languages-30-2ea44f" alt="30 languages">
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT license">
  <img src="https://img.shields.io/badge/size-~150%20KB-lightgrey" alt="about 150 KB">
</p>

<p align="center">
  <a href="README.md">English</a> ·
  <a href="README.ko.md">한국어</a> ·
  <b>日本語</b> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.es.md">Español</a> ·
  <a href="README.de.md">Deutsch</a> ·
  <a href="README.fr.md">Français</a>
</p>

<p align="center">
  <img src="docs/images/settings-dark.png" width="49%" alt="設定画面（ダーク）">
  <img src="docs/images/settings-light.png" width="49%" alt="設定画面（ライト）">
</p>

## なぜ Blinders？

競走馬のブリンカー（遮眼帯）のように、使っているウィンドウだけを明るく保ち、それ以外を背景へ退かせます。ほかのウィンドウやモニター、点滅し続けるチャットはそこにあるまま、静かになります。exe ひとつで動作し、インストール・アカウント・テレメトリーは一切ありません。

## 機能

- **フォーカス調光** — アクティブなウィンドウ以外を全モニターで暗くする（拡大率の異なるモニターにも対応）
- **スポットライト モード** — タスクバーや常に手前のウィンドウも暗くする
- **アクティブなモニターは明るく** — 他のモニターだけを暗くする
- **フルスクリーン対応** — ゲームや全画面動画のあるモニターはそのまま、他は暗く
- **夜間スケジュール** — 指定した時間帯は別の暗さに
- アクティブなウィンドウの**強調枠**（任意）、**色合い**、**フェード**
- **除外アプリ** — アクティブな間は暗くしない。実行中のアプリから選択
- 10分 / 1時間の**一時停止**、**Windows 起動時に開始**
- すべての操作に**ホットキー**（自由に変更可能）
- Windows に合わせた**ライト / ダークの設定画面**、クリックできるモニター配置図
- **30言語**対応、アラビア語・ペルシア語・ヘブライ語は右から左のレイアウト

## ダウンロード

1. [Releases](../../releases) から `Blinders-x.y.z.zip` をダウンロード。
2. 好きな場所に展開して `Blinders.exe` を実行。初回は設定画面が開きます。
3. Windows 10/11 には必要なもの（.NET Framework 4.8）がすでに含まれています。 64 ビット・32 ビットの Windows、ARM64 PC のどれでも同じファイルで動きます。

> まだコード署名がないため、初回は SmartScreen の警告が出ることがあります。**詳細情報 → 実行** を選んでください。

## 使い方

Blinders は時計の横の通知領域（`^`）にいます。アイコンを**左クリック**で設定、**右クリック**でクイックメニュー。アイコンの形でオン / オフ / 一時停止が分かります。

| ホットキー | 動作 |
|---|---|
| `Ctrl` + `Alt` + `Win` + `D` | 暗くする機能のオン / オフ |
| `Ctrl` + `Alt` + `Win` + `↑` / `↓` | もっと暗く / もっと明るく |
| `Ctrl` + `Alt` + `Win` + `E` | 現在のアプリを除外に追加 / 解除 |
| `Ctrl` + `Alt` + `Win` + `Q` | 終了 |

ホットキーは **設定 → ホットキー** で変更できます。既定値は他のアプリのキーを奪わないよう選んでいます。

`Blinders.exe` をもう一度起動すると設定画面が開くだけです。新しいバージョンを起動すると、古いものをきれいに終了して引き継ぎます。

**設定**は `%APPDATA%\Blinders\Blinders.ini` に保存されます。**ポータブル モード:** `Blinders.exe` の隣に `Blinders.ini`（空でも可）を置くと、そのファイルが使われます。

## 言語

English · 简体中文 · 繁體中文 · हिन्दी · Español · Français · العربية · বাংলা · Português · Русский · 日本語 · Deutsch · 한국어 · Türkçe · Tiếng Việt · Italiano · ไทย · Bahasa Indonesia · Polski · Українська · Nederlands · فارسی · עברית · Svenska · Čeština · Română · Ελληνικά · Magyar · Dansk · Suomi

システムの言語が自動で使われます（設定 → その他 → 言語 で変更）。多くの翻訳は下書きなので、**修正を大歓迎します。**

**ビルド不要で言語を修正・追加:** 設定 → その他 → *言語テンプレートを書き出す* で `template.ini` を作り、翻訳して `lang\<コード>.ini`（例: `pt-BR.ini`）として保存し、Blinders を再起動します。足りない行は英語で表示されます。全員に届けるには、[`lang/`](lang) にファイルを追加するプルリクエストを送ってください。

## ソースからビルド

SDK は不要です。Windows に C# コンパイラーが入っています。

```bat
build.bat      :: bin\Blinders.exe
release.bat    :: dist\Blinders-<バージョン>.zip
```

ビルド時にすべての `lang\*.ini` が exe に埋め込まれます。

## ライセンス

[MIT](LICENSE) © 2026 parkjs1362 · [変更履歴](CHANGELOG.md)
