del CNCMaps_*.zip
del CNCMaps_setup_*.exe

set MSBUILD=%WINDIR%\Microsoft.NET\Framework\v3.5\msbuild.exe
set MAKENSIS="%PROGRAMFILES(X86)%\nsis\makensis.exe"

%MSBUILD% CNCMaps.sln /p:Configuration=Release

%MAKENSIS% nsisinstaller.nsi

for /f "delims=" %%a in ('cat nsisinstaller.nsi ^| grep "!define VERSION" ^| gawk "{ print $3 }" ') do @set VER=%%a
 
cd CNCMaps/bin/Release
for /D %%f in (CNCMaps.exe NLog.config NLog.dll OpenGL32.dll OpenTK.dll OpenTK.dll.config osmesa.dll) DO (
	zip -r -j ../../../CNCMaps_v%VER%_win.zip "%%f"
)

for /D %%f in (CNCMaps.exe NLog.config NLog.dll OpenTK.dll OpenTK.dll.config) DO (
	zip -r -j ../../../CNCMaps_v%VER%_nix.zip "%%f"
)

cd ../../../

%MSBUILD% CNCMaps.sln /p:Configuration=Debug
%MAKENSIS% nsisinstaller_debug.nsi