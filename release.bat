@echo off
setlocal
cd /d "%~dp0"
call "%~dp0build.bat"
if errorlevel 1 exit /b 1
for /f %%v in ('powershell -NoProfile -Command "([version](Get-Item bin\Blinders.exe).VersionInfo.FileVersion).ToString(3)"') do set VER=%%v
if not exist dist mkdir dist
powershell -NoProfile -Command "Compress-Archive -Force -Path bin\Blinders.exe,README.md,LICENSE -DestinationPath dist\Blinders-%VER%.zip"
echo Created dist\Blinders-%VER%.zip