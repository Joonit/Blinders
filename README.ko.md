<p align="center">
  <img src="docs/images/logo.png" width="96" alt="Blinders 로고">
</p>

<h1 align="center">Blinders</h1>

<p align="center">
  <b>지금 쓰는 창만 남기고 나머지는 어둡게.</b><br>
  집중을 돕는 작은 Windows 트레이 앱 — 시선이 자꾸 흩어지는 사람, ADHD, 멀티모니터 환경을 위해 만들었습니다.
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
  <b>한국어</b> ·
  <a href="README.ja.md">日本語</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.es.md">Español</a> ·
  <a href="README.de.md">Deutsch</a> ·
  <a href="README.fr.md">Français</a>
</p>

<p align="center">
  <img src="docs/images/settings-dark.png" width="49%" alt="설정 화면, 다크 테마">
  <img src="docs/images/settings-light.png" width="49%" alt="설정 화면, 라이트 테마">
</p>

## 왜 만들었나

경주마의 눈가리개처럼, 지금 쓰는 창은 밝게 두고 나머지는 배경으로 물러나게 합니다. 다른 창, 다른 모니터, 계속 깜빡이는 메신저는 그대로 있지만 조용해집니다. exe 하나로 동작하고, 설치·계정·데이터 수집이 없습니다.

## 기능

- **집중 디밍** — 활성 창을 뺀 나머지를 모든 모니터에서 어둡게 (배율이 다른 모니터 포함)
- **스포트라이트 모드** — 작업 표시줄과 항상 위 창까지 어둡게
- **활성 모니터 밝게 유지** — 다른 모니터만 어둡게
- **전체화면 인식** — 게임이나 전체화면 영상이 있는 모니터는 그대로 두고 나머지만 어둡게
- **야간 일정** — 정한 시간대에는 다른 어둡기 사용
- 활성 창 **강조 테두리**(선택), **색조**, **페이드** 전환
- **제외 앱** — 이 앱이 활성이면 어둡게 하지 않음, 실행 중인 앱에서 선택
- 10분 / 1시간 **일시정지**, **Windows 시작 시 자동 실행**
- 모든 동작에 **단축키**, 자유롭게 변경 가능
- Windows를 따라가는 **라이트 / 다크 설정 창**, 클릭할 수 있는 모니터 배치도
- **30개 언어**, 아랍어·페르시아어·히브리어는 오른쪽에서 왼쪽 레이아웃

## 다운로드

1. [Releases](../../releases)에서 `Blinders-x.y.z.zip`을 받습니다.
2. 아무 곳에나 압축을 풀고 `Blinders.exe`를 실행합니다. 처음 실행하면 설정 창이 열립니다.
3. Windows 10/11에는 필요한 구성 요소(.NET Framework 4.8)가 이미 들어 있습니다. 64비트·32비트 Windows와 ARM64 PC 모두 같은 파일 하나로 실행됩니다.

> 아직 코드 서명이 없어서 처음 실행할 때 SmartScreen 경고가 뜰 수 있습니다. **추가 정보 → 실행**을 누르세요.

## 사용법

Blinders는 시계 옆 알림 영역(`^`)에 있습니다. 아이콘을 **왼쪽 클릭**하면 설정, **오른쪽 클릭**하면 빠른 메뉴가 열립니다. 아이콘 모양으로 켜짐 / 꺼짐 / 일시정지 상태를 알 수 있습니다.

| 단축키 | 동작 |
|---|---|
| `Ctrl` + `Alt` + `Win` + `D` | 어둡게 하기 켜기 / 끄기 |
| `Ctrl` + `Alt` + `Win` + `↑` / `↓` | 더 어둡게 / 더 밝게 |
| `Ctrl` + `Alt` + `Win` + `E` | 현재 앱을 제외 목록에 추가 / 해제 |
| `Ctrl` + `Alt` + `Win` + `Q` | 종료 |

단축키는 **설정 → 단축키**에서 바꿀 수 있습니다. 기본값은 다른 앱이 쓰는 키를 빼앗지 않도록 골랐습니다.

`Blinders.exe`를 다시 실행하면 설정 창만 열립니다. 새 버전을 실행하면 이전 버전을 깔끔하게 닫고 넘겨받습니다.

**설정**은 `%APPDATA%\Blinders\Blinders.ini`에 저장됩니다. **휴대용 모드:** `Blinders.exe` 옆에 `Blinders.ini`(빈 파일이어도 됨)를 두면 그 파일을 사용합니다.

## 언어

English · 简体中文 · 繁體中文 · हिन्दी · Español · Français · العربية · বাংলা · Português · Русский · 日本語 · Deutsch · 한국어 · Türkçe · Tiếng Việt · Italiano · ไทย · Bahasa Indonesia · Polski · Українська · Nederlands · فارسی · עברית · Svenska · Čeština · Română · Ελληνικά · Magyar · Dansk · Suomi

시스템 언어를 자동으로 따릅니다(설정 → 더보기 → 언어에서 변경). 대부분의 번역은 초안이라 **수정 제안을 환영합니다.**

**빌드 없이 언어 수정·추가:** 설정 → 더보기 → *언어 템플릿 내보내기*로 `template.ini`를 만들고 번역한 뒤, `lang\<코드>.ini`(예: `pt-BR.ini`)로 저장하고 Blinders를 다시 시작하세요. 빠진 줄은 영어로 표시됩니다. 모두가 쓰게 하려면 [`lang/`](lang)에 파일을 추가하는 pull request를 보내 주세요.

## 소스에서 빌드

SDK가 필요 없습니다. Windows에 C# 컴파일러가 들어 있습니다.

```bat
build.bat      :: bin\Blinders.exe
release.bat    :: dist\Blinders-<버전>.zip
```

빌드할 때 `lang\*.ini`가 모두 exe에 내장됩니다.

## 라이선스

[MIT](LICENSE) © 2026 parkjs1362 · [변경 이력](CHANGELOG.md)
