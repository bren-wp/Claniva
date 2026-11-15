Unicode true
!include "MUI2.nsh"
!ifndef PUBLISH_DIR
  !error "PUBLISH_DIR required"
!endif
!ifndef OUTPUT_DIR
  !error "OUTPUT_DIR required"
!endif
!ifndef VERSION
  !define VERSION "0.2.0-dev"
!endif

Name "Članarina"
BrandingText "Članarina by BRENDIGO"
SetCompressor /SOLID lzma
RequestExecutionLevel user

!ifdef PORTABLE_BUILD
  OutFile "${OUTPUT_DIR}\Clanarina-Portable.exe"
  SilentInstall silent
  AutoCloseWindow true
  ShowInstDetails nevershow
  Section "Portable"
    InitPluginsDir
    SetOutPath "$PLUGINSDIR\Clanarina"
    File /r "${PUBLISH_DIR}\*.*"
    IfFileExists "$PLUGINSDIR\Clanarina\Clanarina.exe" +2
      Abort "Nedostaje Clanarina.exe"
    ExecWait '"$PLUGINSDIR\Clanarina\Clanarina.exe"' $0
    SetErrorLevel $0
  SectionEnd
!else
  OutFile "${OUTPUT_DIR}\Clanarina-Setup.exe"
  InstallDir "$LOCALAPPDATA\Programs\Clanarina"
  InstallDirRegKey HKCU "Software\BRENDIGO\Clanarina" "InstallPath"
  !define MUI_ABORTWARNING
  !define MUI_FINISHPAGE_RUN "$INSTDIR\Clanarina.exe"
  !insertmacro MUI_PAGE_WELCOME
  !insertmacro MUI_PAGE_LICENSE "${LICENSE_FILE}"
  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_INSTFILES
  !insertmacro MUI_PAGE_FINISH
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  !insertmacro MUI_LANGUAGE "Croatian"

  Section "Članarina" SEC_MAIN
    SetOutPath "$INSTDIR"
    File /r "${PUBLISH_DIR}\*.*"
    WriteRegStr HKCU "Software\BRENDIGO\Clanarina" "InstallPath" "$INSTDIR"
    WriteUninstaller "$INSTDIR\Uninstall.exe"
    CreateDirectory "$SMPROGRAMS\Članarina"
    CreateShortCut "$SMPROGRAMS\Članarina\Članarina.lnk" "$INSTDIR\Clanarina.exe"
    CreateShortCut "$SMPROGRAMS\Članarina\Deinstaliraj.lnk" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Clanarina" "DisplayName" "Članarina"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Clanarina" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Clanarina" "DisplayVersion" "${VERSION}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Clanarina" "Publisher" "BRENDIGO"
  SectionEnd
  Section "Uninstall"
    Delete "$SMPROGRAMS\Članarina\Članarina.lnk"
    Delete "$SMPROGRAMS\Članarina\Deinstaliraj.lnk"
    RMDir "$SMPROGRAMS\Članarina"
    RMDir /r "$INSTDIR"
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Clanarina"
    DeleteRegKey HKCU "Software\BRENDIGO\Clanarina"
    ; Do not delete users' member/payment SQLite data from LocalAppData.
  SectionEnd
!endif
