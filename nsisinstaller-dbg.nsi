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

	; Set Section Files and Shortcuts
	SetOutPath "$INSTDIR\"
	File "CNCMaps.Renderer\bin\${CONFIG}\CNCMaps.Renderer.exe"
	File "CNCMaps.Renderer.GUI\bin\${CONFIG}\CNCMaps.Renderer.GUI.exe"
	File "CNCMaps.Shared\bin\${CONFIG}\CNCMaps.Shared.dll"
	File "CNCMaps.FileFormats\bin\${CONFIG}\CNCMaps.FileFormats.dll"
	File "CNCMaps.Engine\bin\${CONFIG}\CNCMaps.Engine.dll"
	
	; these are no longer used	
	Delete "$INSTDIR\NLog.config"
	Delete "$INSTDIR\NLog.Debug.config"
	
	SetOverwrite ifnewer
	
	File "Lib\NLog.dll"
	;File "Lib\OSMesa.dll"
	File "Lib\OpenTK.dll"
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

	; Clean up Maps Renderer
	Delete "$INSTDIR\CNCMaps.Renderer.exe"
	Delete "$INSTDIR\CNCMaps.Renderer.GUI.exe"
	Delete "$INSTDIR\CNCMaps.Shared.dll"
	Delete "$INSTDIR\CNCMaps.FileFormats.dll"
	Delete "$INSTDIR\CNCMaps.Engine.dll"
	Delete "$INSTDIR\NLog.dll"
	Delete "$INSTDIR\NLog.config"
	Delete "$INSTDIR\NLog.Debug.config"
	Delete "$INSTDIR\opentk.dll"
	Delete "$INSTDIR\osmesa.dll"

	; Remove remaining directories
	RMDir "$SMPROGRAMS\CNCMaps" # old
	RMDir "$INSTDIR\"
SectionEnd

BrandingText "by Frank Razenberg"

; eof