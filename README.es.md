<p align="center">
  <img src="docs/images/logo.png" width="96" alt="Logotipo de Blinders">
</p>

<h1 align="center">Blinders</h1>

<p align="center">
  <b>Atenúa todo excepto la ventana en la que trabajas.</b><br>
  Una pequeña app de bandeja para Windows que ayuda a concentrarse — pensada para miradas que se distraen, el TDAH y escritorios con varios monitores.
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
  <b>Español</b> ·
  <a href="README.de.md">Deutsch</a> ·
  <a href="README.fr.md">Français</a>
</p>

<p align="center">
  <img src="docs/images/settings-dark.png" width="49%" alt="Configuración, tema oscuro">
  <img src="docs/images/settings-light.png" width="49%" alt="Configuración, tema claro">
</p>

## Por qué

Como las anteojeras de un caballo de carreras: la ventana que usas sigue brillante y todo lo demás pasa a segundo plano. Las otras ventanas, los otros monitores, el chat que no deja de parpadear siguen ahí, pero en silencio. Un solo exe, sin instalador, sin cuenta y sin telemetría.

## Funciones

- **Atenuación de enfoque** — todo menos la ventana activa se atenúa, en todos los monitores, incluso con escalas distintas
- **Modo foco** — atenúa también la barra de tareas y las ventanas siempre visibles
- **Mantener brillante el monitor activo** — solo se atenúan los demás monitores
- **Detecta pantalla completa** — el monitor con un juego o un vídeo a pantalla completa se deja tal cual; los demás siguen atenuados
- **Horario nocturno** — otra oscuridad durante las horas que elijas
- **Borde de color** alrededor de la ventana activa (opcional), **tintes** y transiciones con **fundido**
- **Apps excluidas** — sin atenuación mientras están activas; elígelas entre las apps en ejecución
- **Pausa** de 10 minutos o 1 hora, **inicio con Windows**
- **Atajos** para todo, totalmente personalizables
- **Ventana de configuración clara / oscura** que sigue a Windows, con un mapa de monitores en el que se puede hacer clic
- **30 idiomas**, diseño de derecha a izquierda para árabe, persa y hebreo

## Descarga

1. Descarga `Blinders-x.y.z.zip` desde [Releases](../../releases).
2. Descomprímelo donde quieras y ejecuta `Blinders.exe`. La configuración se abre la primera vez.
3. Windows 10/11 ya incluye todo lo necesario (.NET Framework 4.8). La misma descarga funciona en Windows de 64 y 32 bits, incluidos los PC ARM64.

> El exe aún no está firmado, así que SmartScreen puede avisar la primera vez: pulsa **Más información → Ejecutar de todas formas**.

## Uso

Blinders vive en la bandeja del sistema (la zona `^` junto al reloj). **Clic izquierdo** en el icono para la configuración, **clic derecho** para el menú rápido. El icono muestra el estado: activado, desactivado o en pausa.

| Atajo | Acción |
|---|---|
| `Ctrl` + `Alt` + `Win` + `D` | Activar / desactivar la atenuación |
| `Ctrl` + `Alt` + `Win` + `↑` / `↓` | Más oscuro / más claro |
| `Ctrl` + `Alt` + `Win` + `E` | Añadir / quitar la app actual de las exclusiones |
| `Ctrl` + `Alt` + `Win` + `Q` | Salir |

Los atajos se cambian en **Configuración → Atajos**. Los predeterminados están elegidos para no quitarle teclas a otras apps.

Volver a ejecutar `Blinders.exe` solo abre la configuración. Ejecutar una versión más nueva reemplaza la anterior de forma limpia.

La **configuración** se guarda en `%APPDATA%\Blinders\Blinders.ini`. **Modo portátil:** pon un `Blinders.ini` (aunque esté vacío) junto a `Blinders.exe` y se usará ese archivo.

## Idiomas

English · 简体中文 · 繁體中文 · हिन्दी · Español · Français · العربية · বাংলা · Português · Русский · 日本語 · Deutsch · 한국어 · Türkçe · Tiếng Việt · Italiano · ไทย · Bahasa Indonesia · Polski · Українська · Nederlands · فارسی · עברית · Svenska · Čeština · Română · Ελληνικά · Magyar · Dansk · Suomi

Se usa automáticamente el idioma del sistema (Configuración → Más → Idioma para cambiarlo). La mayoría de las traducciones son borradores: **las correcciones son muy bienvenidas.**

**Corregir o añadir un idioma sin compilar:** Configuración → Más → *Exportar plantilla de idioma*, traduce `template.ini`, guárdalo como `lang\<código>.ini` (p. ej. `pt-BR.ini`) y reinicia Blinders. Las líneas que falten se muestran en inglés. Para que llegue a todos, abre un pull request que añada el archivo a [`lang/`](lang).

## Compilar desde el código

No hace falta ningún SDK: Windows incluye el compilador de C#.

```bat
build.bat      :: bin\Blinders.exe
release.bat    :: dist\Blinders-<versión>.zip
```

Todos los `lang\*.ini` se incrustan en el exe al compilar.

## Licencia

[MIT](LICENSE) © 2026 parkjs1362 · [Historial de cambios](CHANGELOG.md)
