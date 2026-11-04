<p align="center">
  <img src="docs/images/logo.png" width="96" alt="Blinders 标志">
</p>

<h1 align="center">Blinders</h1>

<p align="center">
  <b>只留下你正在用的窗口，其余全部调暗。</b><br>
  一个帮助专注的小巧 Windows 托盘应用——为容易分心的人、ADHD 和多显示器桌面而做。
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
  <a href="README.ja.md">日本語</a> ·
  <b>简体中文</b> ·
  <a href="README.es.md">Español</a> ·
  <a href="README.de.md">Deutsch</a> ·
  <a href="README.fr.md">Français</a>
</p>

<p align="center">
  <img src="docs/images/settings-dark.png" width="49%" alt="设置界面（深色）">
  <img src="docs/images/settings-light.png" width="49%" alt="设置界面（浅色）">
</p>

## 为什么

就像赛马的眼罩：你正在使用的窗口保持明亮，其余的一切退到背景里。其他窗口、其他显示器、不停闪烁的聊天——都还在，只是安静了。一个 exe 即可运行，无需安装、无需账号、没有遥测。

## 功能

- **专注调暗**——在所有显示器上调暗活动窗口以外的区域（支持不同缩放比例）
- **聚光灯模式**——同时调暗任务栏和置顶窗口
- **保持活动显示器明亮**——只调暗其他显示器
- **识别全屏**——有游戏或全屏视频的显示器保持原样，其他继续调暗
- **夜间计划**——在你选定的时段使用不同暗度
- 活动窗口的**强调边框**（可选）、**色调**、**渐变**过渡
- **排除的应用**——这些应用处于活动状态时不调暗，可从正在运行的应用中选择
- **暂停** 10 分钟或 1 小时，**开机自动启动**
- 所有操作都有**快捷键**，可完全自定义
- 跟随 Windows 的**浅色 / 深色设置窗口**，以及可点击的显示器布局图
- **30 种语言**，阿拉伯语、波斯语和希伯来语使用从右到左布局

## 下载

1. 从 [Releases](../../releases) 下载 `Blinders-x.y.z.zip`。
2. 解压到任意位置并运行 `Blinders.exe`。首次运行会打开设置。
3. Windows 10/11 已自带所需组件（.NET Framework 4.8）。同一个文件即可在 64 位、32 位 Windows 以及 ARM64 电脑上运行。

> exe 尚未进行代码签名，首次运行时 SmartScreen 可能会提示：点击 **更多信息 → 仍要运行**。

## 使用

Blinders 位于时钟旁的系统托盘（`^` 区域）。**左键单击**图标打开设置，**右键单击**打开快捷菜单。图标会显示状态：开启、关闭或暂停。

| 快捷键 | 操作 |
|---|---|
| `Ctrl` + `Alt` + `Win` + `D` | 开启 / 关闭调暗 |
| `Ctrl` + `Alt` + `Win` + `↑` / `↓` | 更暗 / 更亮 |
| `Ctrl` + `Alt` + `Win` + `E` | 将当前应用加入 / 移出排除列表 |
| `Ctrl` + `Alt` + `Win` + `Q` | 退出 |

快捷键可在 **设置 → 快捷键** 中修改。默认组合经过挑选，不会占用其他应用常用的按键。

再次运行 `Blinders.exe` 只会打开设置。运行更新的版本会干净地关闭旧版本并接管。

**设置**保存在 `%APPDATA%\Blinders\Blinders.ini`。**便携模式：**在 `Blinders.exe` 旁边放一个 `Blinders.ini`（空文件也可以），就会改用这个文件。

## 语言

English · 简体中文 · 繁體中文 · हिन्दी · Español · Français · العربية · বাংলা · Português · Русский · 日本語 · Deutsch · 한국어 · Türkçe · Tiếng Việt · Italiano · ไทย · Bahasa Indonesia · Polski · Українська · Nederlands · فارسی · עברית · Svenska · Čeština · Română · Ελληνικά · Magyar · Dansk · Suomi

自动使用系统语言（在 设置 → 更多 → 语言 中更改）。大多数翻译还是初稿——**非常欢迎指正。**

**无需编译即可修正或添加语言：**设置 → 更多 → *导出语言模板*，翻译 `template.ini`，另存为 `lang\<代码>.ini`（例如 `pt-BR.ini`）后重启 Blinders。缺少的行会显示英文。想让所有人都用上，请提交一个向 [`lang/`](lang) 添加该文件的 pull request。

## 从源码构建

无需 SDK——Windows 自带 C# 编译器。

```bat
build.bat      :: bin\Blinders.exe
release.bat    :: dist\Blinders-<版本>.zip
```

构建时会把所有 `lang\*.ini` 嵌入 exe。

## 许可证

[MIT](LICENSE) © 2026 parkjs1362 · [更新日志](CHANGELOG.md)
