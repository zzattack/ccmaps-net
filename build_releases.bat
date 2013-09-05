set VER=2.0beta28

del CNCMaps_*.zip
del CNCMaps_setup_*.exe
del CNCMaps/bin/*.*
del CNCMaps/obj/*.*
del CNCMaps GUI/obj/*.*
del CNCMaps GUI/obj/*.*

set MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set MAKENSIS="%PROGRAMFILES(X86)%\nsis\makensis.exe"

%MSBUILD% CNCMaps.sln /p:Configuration=Release
%MAKENSIS% nsisinstaller-rls.nsi
 
cd CNCMaps/bin/Release
for /D %%f in (CNCMaps.exe NLog.config NLog.dll OpenTK.dll OpenTK.dll.config osmesa.dll) DO (
	# zip -r -j ../../../CNCMaps_v%VER%_win.zip "%%f"
)

for /D %%f in (CNCMaps.exe NLog.config NLog.dll OpenTK.dll OpenTK.dll.config) DO (
	# zip -r -j ../../../CNCMaps_v%VER%_nix.zip "%%f"
)

cd ../../../

exit

%MSBUILD% CNCMaps.sln /p:Configuration=Debug
%MAKENSIS% nsisinstaller-dbg.nsi