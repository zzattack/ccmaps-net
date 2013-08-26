!ifndef CONFIG
!define CONFIG "debug"
!endif
!include LogicLib.nsh

; Define your application name
!define APPNAME "CNC Map Render"
!define VERSION $%VER% ; environment variable, call from .bat file
!define APPNAMEANDVERSION "CNC Map Render ${VERSION}"

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
!define MUI_FINISHPAGE_RUN "$INSTDIR\CNCMaps_GUI.exe"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Set languages (first is default language)
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_RESERVEFILE_LANGDLL

Section "RA2/YR Maps Renderer" Section1

	; Set Section properties
	SetOverwrite on

	; Set Section Files and Shortcuts
	SetOutPath "$INSTDIR\"
	File "CNCMaps\bin\${CONFIG}\CNCMaps.exe"
	File "CNCMaps GUI\bin\${CONFIG}\CNCMaps_GUI.exe"
	File "CNCMaps\NLog.dll"	
	${If} ${CONFIG} == "Debug"
		File "CNCMaps\NLog.Debug.config"
	${Else}
		File "CNCMaps\NLog.config"
	${EndIf}
	File "CNCMaps\OSMesa.dll"
	File "CNCMaps\OpenTK.dll"
	File "CNCMaps\OpenTK.dll.config"
	CreateShortCut "$DESKTOP\CNC Maps renderer.lnk" "$INSTDIR\CNCMaps_GUI.exe"
	CreateDirectory "$SMPROGRAMS\CNC Maps renderer"
	CreateShortCut "$SMPROGRAMS\CNC Maps renderer\CNC Maps renderer.lnk" "$INSTDIR\CNCMaps_GUI.exe"
	CreateShortCut "$SMPROGRAMS\CNC Maps renderer\Uninstall.lnk" "$INSTDIR\uninstall.exe"

SectionEnd

Section -FinishSection
	WriteRegStr HKLM "Software\${APPNAME}" "" "$INSTDIR"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$INSTDIR\uninstall.exe"
	WriteUninstaller "$INSTDIR\uninstall.exe"

SectionEnd

; Modern install component descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${Section1} ""
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;Uninstall section
Section Uninstall

	;Remove from registry...
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
	DeleteRegKey HKLM "SOFTWARE\${APPNAME}"

	; Delete self
	Delete "$INSTDIR\uninstall.exe"

	; Delete Shortcuts
	Delete "$DESKTOP\CNC Maps Renderer.lnk"
	Delete "$SMPROGRAMS\CNC Maps Renderer\CNC Maps renderer.lnk"
	Delete "$SMPROGRAMS\CNC Maps Renderer\Uninstall.lnk"

	; Clean up RA2/YR Maps renderer
	Delete "$INSTDIR\CNCMaps.exe"
	Delete "$INSTDIR\CNCMaps_GUI.exe"
	Delete "$INSTDIR\NLog.dll"
	Delete "$INSTDIR\NLog.Debug.config"
	Delete "$INSTDIR\opengl32.dll"
	Delete "$INSTDIR\OpenTK.dll"
	Delete "$INSTDIR\OpenTK.dll.config"
	Delete "$INSTDIR\osmesa.dll"

	; Remove remaining directories
	RMDir "$SMPROGRAMS\CNC Maps renderer"
	RMDir "$INSTDIR\"

SectionEnd

BrandingText "by Frank Razenberg"

; eof