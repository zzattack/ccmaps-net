set MSBUILD=%WINDIR%\Microsoft.NET\Framework\v3.5\msbuild.exe
set MAKENSIS="%PROGRAMFILES(X86)%\nsis\makensis.exe"

%MSBUILD% CNCMaps.sln /p:Configuration=Release

%MAKENSIS% nsisinstaller.nsi

for /f "delims=" %%a in ('cat nsisinstaller.nsi ^| grep "!define VERSION" ^| gawk "{ print $3 }" ') do @set VER=%%a
 
cd CNCMaps/bin/Release
for /D %%f in (CNCMaps.exe NLog.config NLog.dll OpenlGL32.dll OpenTK.dll OpenTK.dll.config osmesa.dll) DO (
	zip -r -j ../../../Release_v%VER%.zip "%%f"
)
