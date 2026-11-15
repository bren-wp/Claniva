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

!ifdef DEMO_BUILD
  !define PRODUCT_TITLE "Članarina DEMO"
  !define PACKAGE_NAME "Clanarina-Demo"
  !define APP_FOLDER "ClanarinaDemo"
  !define APP_EXECUTABLE "ClanarinaDemo.exe"
  !define SHORTCUT_NAME "Članarina DEMO"
!else
  !define PRODUCT_TITLE "Članarina"
  !define PACKAGE_NAME "Clanarina"
  !define APP_FOLDER "Clanarina"
  !define APP_EXECUTABLE "Clanarina.exe"
  !define SHORTCUT_NAME "Članarina"
!endif
Name "${PRODUCT_TITLE}"
BrandingText "Članarina by BRENDIGO"

SetCompressor /SOLID lzma
RequestExecutionLevel user

!ifdef PORTABLE_BUILD
  OutFile "${OUTPUT_DIR}\${PACKAGE_NAME}-Portable.exe"
  SilentInstall silent
  AutoCloseWindow true
  ShowInstDetails nevershow
  Section "Portable"
    InitPluginsDir
    SetOutPath "$PLUGINSDIR\Clanarina"
    File /r "${PUBLISH_DIR}\*.*"
    IfFileExists "$PLUGINSDIR\Clanarina\${APP_EXECUTABLE}" +2
      Abort "Nedostaje glavna aplikacija"
    ExecWait '"$PLUGINSDIR\Clanarina\${APP_EXECUTABLE}"' $0
    SetErrorLevel $0
  SectionEnd
!else
  OutFile "${OUTPUT_DIR}\${PACKAGE_NAME}-Setup.exe"
  InstallDir "$LOCALAPPDATA\Programs\${APP_FOLDER}"
  InstallDirRegKey HKCU "Software\BRENDIGO\${APP_FOLDER}" "InstallPath"
  !define MUI_ABORTWARNING
  !define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXECUTABLE}"
  !insertmacro MUI_PAGE_WELCOME
  !insertmacro MUI_PAGE_LICENSE "${LICENSE_FILE}"
  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_INSTFILES
  !insertmacro MUI_PAGE_FINISH
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  !insertmacro MUI_LANGUAGE "Croatian"

  Section "${PRODUCT_TITLE}" SEC_MAIN
    SetOutPath "$INSTDIR"
    File /r "${PUBLISH_DIR}\*.*"
    WriteRegStr HKCU "Software\BRENDIGO\${APP_FOLDER}" "InstallPath" "$INSTDIR"
    WriteUninstaller "$INSTDIR\Uninstall.exe"
    CreateDirectory "$SMPROGRAMS\${APP_FOLDER}"
    CreateShortCut "$SMPROGRAMS\${APP_FOLDER}\${SHORTCUT_NAME}.lnk" "$INSTDIR\${APP_EXECUTABLE}"
    CreateShortCut "$SMPROGRAMS\${APP_FOLDER}\Deinstaliraj.lnk" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_FOLDER}" "DisplayName" "${PRODUCT_TITLE}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_FOLDER}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_FOLDER}" "DisplayVersion" "${VERSION}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_FOLDER}" "Publisher" "BRENDIGO"
  SectionEnd
  Section "Uninstall"
    Delete "$SMPROGRAMS\${APP_FOLDER}\${SHORTCUT_NAME}.lnk"
    Delete "$SMPROGRAMS\${APP_FOLDER}\Deinstaliraj.lnk"
    RMDir "$SMPROGRAMS\${APP_FOLDER}"
    RMDir /r "$INSTDIR"
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_FOLDER}"
    DeleteRegKey HKCU "Software\BRENDIGO\${APP_FOLDER}"
    ; Do not delete users' member/payment SQLite data from LocalAppData.
  SectionEnd
!endif
