set VER=2.4.0
set MAKENSIS="%PROGRAMFILES(X86)%\nsis\makensis.exe"

del CNCMaps_*.zip
del CNCMaps_setup_*.exe
del CNCMaps/bin/*.*
del CNCMaps/obj/*.*
del CNCMaps GUI/obj/*.*
del CNCMaps GUI/obj/*.*

set VSINSTALLDIR=C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\
set DevEnvDir=C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat"
msbuild kerfcheck.sln 

msbuild CNCMaps.sln /t:restore /t:Build /p:Configuration=Release
%MAKENSIS% nsisinstaller-rls.nsi

cd CNCMaps/bin/Release
for /D %%f in (*.exe  *.dll NLog.config) DO (
	# zip -r -j ../../../CNCMaps_v%VER%_win.zip "%%f"
)

for /D %%f in (*.exe  *.dll NLog.config OpenTK.dll.config) DO (
	# zip -r -j ../../../CNCMaps_v%VER%_nix.zip "%%f"
)

cd ../../../

%MSBUILD% CNCMaps.sln /p:Configuration=Debug
%MAKENSIS% nsisinstaller-dbg.nsi