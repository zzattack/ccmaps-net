set VER=2.0beta1

del CNCMaps_*.zip
del CNCMaps_setup_*.exe

set MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set MAKENSIS="%PROGRAMFILES(X86)%\nsis\makensis.exe"

%MSBUILD% CNCMaps.sln /p:Configuration=Release
%MAKENSIS% nsisinstaller-rls.nsi

for /f "delims=" %%a in ('cat nsisinstaller-dbg.nsi ^| grep "!define VERSION" ^| gawk "{ print $3 }" ') do @set VER=%%a
 
cd CNCMaps/bin/Release
for /D %%f in (CNCMaps.exe NLog.config NLog.dll OpenTK.dll OpenTK.dll.config osmesa.dll) DO (
	zip -r -j ../../../CNCMaps_v%VER%_win.zip "%%f"
)

for /D %%f in (CNCMaps.exe NLog.config NLog.dll OpenTK.dll OpenTK.dll.config) DO (
	zip -r -j ../../../CNCMaps_v%VER%_nix.zip "%%f"
)

cd ../../../

%MSBUILD% CNCMaps.sln /p:Configuration=Debug
%MAKENSIS% nsisinstaller-dbg.nsi