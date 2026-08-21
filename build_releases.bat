@echo off
set VER=2.4.0
set MAKENSIS="%PROGRAMFILES(X86)%\nsis\makensis.exe"

del CNCMaps_release_*.exe CNCMaps_console_*.zip 2>nul
rmdir /s /q publish 2>nul

rem Self-contained so end users need no .NET runtime install
dotnet publish CNCMaps.Renderer -c Release -r win-x64 --self-contained -o publish\release || exit /b 1
dotnet publish CNCMaps.Renderer.GUI -c Release -r win-x64 --self-contained -o publish\release || exit /b 1

rem Trimmed console-only zip for mods that bundle the renderer
dotnet publish CNCMaps.Renderer -c Release -r win-x64 --self-contained -p:PublishTrimmed=true -o publish\console || exit /b 1

powershell -NoProfile -Command "Compress-Archive -Force publish\console\* CNCMaps_console_v%VER%.zip" || exit /b 1
%MAKENSIS% nsisinstaller-rls.nsi
