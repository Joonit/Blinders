@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"
if not exist bin mkdir bin
rem every lang\*.ini is embedded into the exe as Blinders.lang.<code>.ini
set RES=
for %%f in (lang\*.ini) do set RES=!RES! -resource:lang\%%~nxf,Blinders.lang.%%~nxf
set ICON=
if exist assets\Blinders.ico set ICON=-win32icon:assets\Blinders.ico
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" -nologo -target:winexe -codepage:65001 -optimize+ -out:bin\Blinders.exe -r:System.dll -r:System.Core.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll !ICON! !RES! *.cs