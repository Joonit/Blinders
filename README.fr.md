<p align="center">
  <img src="docs/images/logo.png" width="96" alt="Logo de Blinders">
</p>

<h1 align="center">Blinders</h1>

<p align="center">
  <b>Assombrit tout, sauf la fenêtre dans laquelle vous travaillez.</b><br>
  Une petite application Windows de la zone de notification pour rester concentré — pensée pour les regards qui s'égarent, le TDAH et les bureaux à plusieurs écrans.
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
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.es.md">Español</a> ·
  <a href="README.de.md">Deutsch</a> ·
  <b>Français</b>
</p>

<p align="center">
  <img src="docs/images/settings-dark.png" width="49%" alt="Paramètres, thème sombre">
  <img src="docs/images/settings-light.png" width="49%" alt="Paramètres, thème clair">
</p>

## Pourquoi

Comme les œillères d'un cheval de course : la fenêtre que vous utilisez reste lumineuse, tout le reste passe à l'arrière-plan. Les autres fenêtres, les autres écrans, la messagerie qui clignote sans cesse sont toujours là, simplement silencieux. Un seul exe, sans installation, sans compte, sans télémétrie.

## Fonctionnalités

- **Assombrissement de concentration** — tout sauf la fenêtre active est assombri, sur chaque écran, y compris avec des mises à l'échelle différentes
- **Mode projecteur** — assombrit aussi la barre des tâches et les fenêtres toujours au premier plan
- **Garder l'écran actif lumineux** — seuls les autres écrans sont assombris
- **Gestion du plein écran** — l'écran qui affiche un jeu ou une vidéo plein écran reste intact, les autres restent assombris
- **Planification de nuit** — une autre obscurité aux heures choisies
- **Bordure colorée** autour de la fenêtre active (optionnelle), **teintes**, transitions en **fondu**
- **Applications exclues** — pas d'assombrissement tant qu'elles sont actives ; choix parmi les applications ouvertes
- **Pause** de 10 minutes ou d'une heure, **démarrage avec Windows**
- **Raccourcis** pour tout, entièrement personnalisables
- **Fenêtre de paramètres claire / sombre** qui suit Windows, avec une carte des écrans cliquable
- **30 langues**, mise en page de droite à gauche pour l'arabe, le persan et l'hébreu

## Téléchargement

1. Téléchargez `Blinders-x.y.z.zip` depuis [Releases](../../releases).
2. Décompressez-le où vous voulez et lancez `Blinders.exe`. Les paramètres s'ouvrent au premier lancement.
3. Windows 10/11 contient déjà tout le nécessaire (.NET Framework 4.8). Un seul téléchargement pour Windows 64 et 32 bits, y compris les PC ARM64.

> L'exe n'est pas encore signé, SmartScreen peut donc avertir au premier lancement : cliquez sur **Informations complémentaires → Exécuter quand même**.

## Utilisation

Blinders se trouve dans la zone de notification (la zone `^` près de l'horloge). **Clic gauche** sur l'icône pour les paramètres, **clic droit** pour le menu rapide. L'icône indique l'état : activé, désactivé ou en pause.

| Raccourci | Action |
|---|---|
| `Ctrl` + `Alt` + `Win` + `D` | Activer / désactiver l'assombrissement |
| `Ctrl` + `Alt` + `Win` + `↑` / `↓` | Plus sombre / plus clair |
| `Ctrl` + `Alt` + `Win` + `E` | Ajouter / retirer l'application actuelle des exclusions |
| `Ctrl` + `Alt` + `Win` + `Q` | Quitter |

Les raccourcis se modifient dans **Paramètres → Raccourcis**. Ceux par défaut sont choisis pour ne pas voler de touches aux autres applications.

Relancer `Blinders.exe` ouvre simplement les paramètres. Lancer une version plus récente remplace proprement l'ancienne.

Les **paramètres** sont enregistrés dans `%APPDATA%\Blinders\Blinders.ini`. **Mode portable :** placez un `Blinders.ini` (même vide) à côté de `Blinders.exe` et c'est ce fichier qui sera utilisé.

## Langues

English · 简体中文 · 繁體中文 · हिन्दी · Español · Français · العربية · বাংলা · Português · Русский · 日本語 · Deutsch · 한국어 · Türkçe · Tiếng Việt · Italiano · ไทย · Bahasa Indonesia · Polski · Українська · Nederlands · فارسی · עברית · Svenska · Čeština · Română · Ελληνικά · Magyar · Dansk · Suomi

La langue du système est utilisée automatiquement (Paramètres → Plus → Langue pour la changer). La plupart des traductions sont des premiers jets — **les corrections sont les bienvenues.**

**Corriger ou ajouter une langue sans compiler :** Paramètres → Plus → *Exporter le modèle de langue*, traduisez `template.ini`, enregistrez-le sous `lang\<code>.ini` (par exemple `pt-BR.ini`) et redémarrez Blinders. Les lignes manquantes s'affichent en anglais. Pour en faire profiter tout le monde, ouvrez une pull request qui ajoute le fichier dans [`lang/`](lang).

## Compiler depuis les sources

Aucun SDK requis : Windows fournit le compilateur C#.

```bat
build.bat      :: bin\Blinders.exe
release.bat    :: dist\Blinders-<version>.zip
```

Tous les fichiers `lang\*.ini` sont intégrés à l'exe lors de la compilation.

## Licence

[MIT](LICENSE) © 2026 parkjs1362 · [Journal des modifications](CHANGELOG.md)
