Unicode true
!include "MUI2.nsh"
!include "StrFunc.nsh"
${StrStr}

!ifndef APP_VERSION
  !define APP_VERSION "0.1.0-alpha"
!endif
!ifndef APP_VERSION_NUM
  !define APP_VERSION_NUM "0.1.0.0"
!endif
!ifndef PUBLISH_DIR
  !error "PUBLISH_DIR no está definido."
!endif
!ifndef OUTPUT_DIR
  !error "OUTPUT_DIR no está definido."
!endif

Name "DeskNotes"
Caption "Instalar DeskNotes"
OutFile "${OUTPUT_DIR}\desknotes-setupe.exe"
InstallDir "$LOCALAPPDATA\Programs\DeskNotes"
InstallDirRegKey HKCU "Software\DeskNotes" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
ShowInstDetails show
ShowUnInstDetails show

VIProductVersion "${APP_VERSION_NUM}"
VIAddVersionKey "ProductName" "DeskNotes"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"
VIAddVersionKey "FileDescription" "Instalador de DeskNotes"
VIAddVersionKey "FileVersion" "${APP_VERSION}"

!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"
!define MUI_WELCOMEPAGE_TITLE "Instalar DeskNotes"
!define MUI_WELCOMEPAGE_TEXT "El asistente instalará DeskNotes en tu perfil de usuario. No se necesitan permisos de administrador.$\r$\n$\r$\nDeskNotes requiere Microsoft .NET 10 Desktop Runtime x64."
!define MUI_FINISHPAGE_RUN "$INSTDIR\DeskNotes.App.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Abrir DeskNotes"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

Function .onInit
  SetShellVarContext current
  nsExec::ExecToStack 'dotnet --list-runtimes'
  Pop $0
  Pop $1
  ${StrStr} $2 $1 "Microsoft.WindowsDesktop.App 10."
  StrCmp $0 "0" 0 runtime_missing
  StrCmp $2 "" runtime_missing runtime_ok

runtime_missing:
  MessageBox MB_ICONEXCLAMATION|MB_YESNO "DeskNotes necesita Microsoft .NET 10 Desktop Runtime (x64), que no se incluye en este instalador.$\r$\n$\r$\n¿Quieres abrir la página oficial para instalarlo?" IDYES open_runtime_page IDNO cancel_install
open_runtime_page:
  ExecShell "open" "https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
cancel_install:
  Abort
runtime_ok:
FunctionEnd

Section "DeskNotes" SecMain
  SectionIn RO
  SetShellVarContext current
  SetOutPath "$INSTDIR"
  File "${PUBLISH_DIR}\DeskNotes.App.exe"

  CreateDirectory "$SMPROGRAMS\DeskNotes"
  CreateShortcut "$SMPROGRAMS\DeskNotes\DeskNotes.lnk" "$INSTDIR\DeskNotes.App.exe"
  CreateShortcut "$SMPROGRAMS\Desinstalar DeskNotes.lnk" "$INSTDIR\Uninstall.exe"

  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\DeskNotes" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes" "DisplayName" "DeskNotes"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes" "NoRepair" 1
SectionEnd

Section /o "Crear acceso directo en el escritorio" SecDesktop
  SetShellVarContext current
  CreateShortcut "$DESKTOP\DeskNotes.lnk" "$INSTDIR\DeskNotes.App.exe"
SectionEnd

Section "Uninstall"
  SetShellVarContext current
  Delete "$DESKTOP\DeskNotes.lnk"
  Delete "$SMPROGRAMS\DeskNotes\DeskNotes.lnk"
  RMDir "$SMPROGRAMS\DeskNotes"
  Delete "$SMPROGRAMS\Desinstalar DeskNotes.lnk"
  Delete "$INSTDIR\DeskNotes.App.exe"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"

  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes"
  DeleteRegKey HKCU "Software\DeskNotes"
  ; No borrar %LOCALAPPDATA%\DeskNotes: contiene ajustes y datos del usuario.
SectionEnd
