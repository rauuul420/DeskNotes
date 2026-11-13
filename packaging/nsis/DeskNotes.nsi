Unicode True

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"

!ifndef APP_NAME
  !define APP_NAME "DeskNotes"
!endif
!ifndef APP_VERSION
  !define APP_VERSION "0.1.0-alpha"
!endif
!ifndef APP_VERSION_NUM
  !define APP_VERSION_NUM "0.1.0.0"
!endif
!ifndef APP_FILE_VERSION
  !define APP_FILE_VERSION "0.1.0"
!endif
!ifndef PUBLISH_DIR
  !error "PUBLISH_DIR no está definido."
!endif
!ifndef OUTPUT_DIR
  !error "OUTPUT_DIR no está definido."
!endif

!define APP_EXE "DeskNotes.App.exe"
!define APP_UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes"

Name "${APP_NAME} ${APP_VERSION}"
Caption "Instalar ${APP_NAME}"
OutFile "${OUTPUT_DIR}\DeskNotes-Setup-${APP_FILE_VERSION}.exe"
InstallDir "$PROGRAMFILES64\DeskNotes"
RequestExecutionLevel admin

SetCompressor /SOLID lzma
ShowInstDetails show
ShowUnInstDetails show

VIProductVersion "${APP_VERSION_NUM}"
VIAddVersionKey "ProductName" "${APP_NAME}"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"
VIAddVersionKey "CompanyName" "DeskNotes"
VIAddVersionKey "FileDescription" "Instalador de DeskNotes"
VIAddVersionKey "FileVersion" "${APP_VERSION_NUM}"
VIAddVersionKey "LegalCopyright" "Copyright 2026 DeskNotes"

!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"
!define MUI_WELCOMEPAGE_TITLE "Instalar DeskNotes"
!define MUI_WELCOMEPAGE_TEXT "El asistente instalará DeskNotes en Archivos de programa. Se necesitan permisos de administrador.$\r$\n$\r$\nDeskNotes requiere Microsoft .NET 10 Desktop Runtime x64, que no se incluye en el instalador."
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Iniciar DeskNotes"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Spanish"

; ============================================================
; COMPROBACIÓN DE .NET 10 DESKTOP RUNTIME X64
; ============================================================

Function .onInit

    ${IfNot} ${RunningX64}
        MessageBox MB_ICONSTOP|MB_OK "DeskNotes requiere Windows de 64 bits."
        Abort
    ${EndIf}

    SetRegView 64

    FindFirst $0 $1 "$PROGRAMFILES64\dotnet\shared\Microsoft.WindowsDesktop.App\10.*"
    StrCmp $1 "" runtime_missing

    FindClose $0
    Goto runtime_found

runtime_missing:
    MessageBox MB_ICONEXCLAMATION|MB_YESNO "DeskNotes necesita Microsoft .NET 10 Desktop Runtime x64.$\r$\n$\r$\nNo se ha encontrado una instalación compatible.$\r$\n$\r$\n¿Deseas abrir la página oficial para descargarlo?" IDYES download_runtime IDNO cancel_install

download_runtime:
    ExecShell "open" "https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
    MessageBox MB_ICONINFORMATION|MB_OK "Instala .NET 10 Desktop Runtime x64 y vuelve a ejecutar el instalador de DeskNotes."
    Abort

cancel_install:
    Abort

runtime_found:

FunctionEnd

; ============================================================
; INSTALACIÓN
; ============================================================

Section "DeskNotes" SEC_MAIN

    SetRegView 64
    SetShellVarContext all

    SetOutPath "$INSTDIR"

    ; Incluye el EXE y los recursos satélite de publicación.
    File /r "${PUBLISH_DIR}\*.*"

    ; Accesos directos
    CreateDirectory "$SMPROGRAMS\DeskNotes"
    CreateShortcut "$SMPROGRAMS\DeskNotes\DeskNotes.lnk" "$INSTDIR\${APP_EXE}"
    CreateShortcut "$DESKTOP\DeskNotes.lnk" "$INSTDIR\${APP_EXE}"

    ; Desinstalador
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Registro de aplicaciones instaladas de Windows
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "DisplayName" "${APP_NAME}"
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "Publisher" "DeskNotes"
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegDWORD HKLM "${APP_UNINSTALL_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${APP_UNINSTALL_KEY}" "NoRepair" 1

SectionEnd

; ============================================================
; DESINSTALACIÓN
; ============================================================

Section "Uninstall"

    SetRegView 64
    SetShellVarContext all

    Delete "$DESKTOP\DeskNotes.lnk"
    Delete "$SMPROGRAMS\DeskNotes\DeskNotes.lnk"
    RMDir "$SMPROGRAMS\DeskNotes"

    DeleteRegKey HKLM "${APP_UNINSTALL_KEY}"

    Delete "$INSTDIR\Uninstall.exe"
    RMDir /r "$INSTDIR"

    ; No se elimina %LOCALAPPDATA%\DeskNotes:
    ; contiene los ajustes y datos locales del usuario.

SectionEnd
