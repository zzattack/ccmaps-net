!ifndef CONFIG
!define CONFIG "debug"
!endif
!include LogicLib.nsh
!include Sections.nsh

; Define your application name
!define APPNAME "CNCMaps"
!define VERSION $%VER% ; environment variable, call from .bat file
!define APPNAMEANDVERSION "CNCMaps ${VERSION}"

; Main Install settings
Name "${APPNAMEANDVERSION}"
InstallDir "$PROGRAMFILES\CNCMaps"
InstallDirRegKey HKLM "Software\${APPNAME}" ""
OutFile "CNCMaps_${CONFIG}_v${VERSION}.exe"

; Use compression
SetCompressor LZMA

; Modern interface settings
!include "MUI.nsh"

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\CNCMaps.Renderer.GUI.exe"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Set languages (first is default language)
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_RESERVEFILE_LANGDLL

Section "Maps Renderer" sec_program

	; Set Section properties
	SetOverwrite on

	; Set Section Files and Shortcuts (produced by dotnet publish, see build_releases.bat)
	SetOutPath "$INSTDIR\"
	File /r "publish\${CONFIG}\*"
SectionEnd

Section /o "Start menu shortcuts" sec_shortcut_startmenu
	CreateShortCut "$SMPROGRAMS\CNC Maps renderer.lnk" "$INSTDIR\CNCMaps.Renderer.GUI.exe"
SectionEnd

Section /o "Desktop shortcut" sec_shortcut_desktop
	CreateShortCut "$DESKTOP\CNCMaps Renderer.lnk" "$INSTDIR\CNCMaps.Renderer.GUI.exe"
SectionEnd

Section -FinishSection
	WriteRegStr HKLM "Software\${APPNAME}" "" "$INSTDIR"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$INSTDIR\uninstall.exe"
	WriteUninstaller "$INSTDIR\uninstall.exe"

SectionEnd

; Modern install component descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${sec_program} ""
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;Uninstall section
Section Uninstall

	;Remove from registry...
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
	DeleteRegKey HKLM "SOFTWARE\${APPNAME}"

	; Delete self
	Delete "$INSTDIR\uninstall.exe"

	; Delete Shortcuts
	Delete "$DESKTOP\CNCMaps Renderer.lnk" 
	Delete "$SMPROGRAMS\CNC Maps renderer.lnk"
	Delete "$SMPROGRAMS\CNCMaps\CNC Maps renderer.lnk" # old locations
	Delete "$SMPROGRAMS\CNCMaps\Uninstall.lnk"

	; Clean up Maps Renderer (entire publish output)
	RMDir /r "$INSTDIR\"

	; Remove remaining directories
	RMDir "$SMPROGRAMS\CNCMaps" # old
SectionEnd

BrandingText "by Frank Razenberg"

; eof