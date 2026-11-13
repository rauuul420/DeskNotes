
; ============================================================
; DeskNotes - Windows Installer
; Version: 0.1.0-alpha
; Architecture: Windows x64
; Installer: NSIS + Modern UI 2
; ============================================================

Unicode True
!cd ".."

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"

!define APP_NAME "DeskNotes"
!define APP_VERSION "0.1.0-alpha"
!define APP_VERSION_NUM "0.1.0.0"
!define APP_PUBLISHER "DeskNotes"
!define APP_EXE "DeskNotes.App.exe"
!define APP_UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskNotes"

!define PUBLISH_DIR "src\DeskNotes.App\bin\Release\net10.0-windows\win-x64\publish"

Name "${APP_NAME} ${APP_VERSION}"
OutFile "installer\DeskNotes-Setup-0.1.0.exe"
InstallDir "$PROGRAMFILES64\DeskNotes"
RequestExecutionLevel admin

SetCompressor /SOLID lzma
SetCompressorDictSize 64

VIProductVersion "${APP_VERSION_NUM}"
VIAddVersionKey "ProductName" "${APP_NAME}"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"
VIAddVersionKey "CompanyName" "${APP_PUBLISHER}"
VIAddVersionKey "FileDescription" "DeskNotes Windows Installer"
VIAddVersionKey "FileVersion" "${APP_VERSION_NUM}"
VIAddVersionKey "LegalCopyright" "Copyright 2026 DeskNotes"

; ============================================================
; INTERFAZ
; ============================================================

!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Iniciar DeskNotes"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Spanish"

; ============================================================
; COMPROBACIÓN DE REQUISITOS
; ============================================================


; ============================================================
; COMPROBACIÓN DE .NET 10 DESKTOP RUNTIME X64
; ============================================================

Function .onInit

    ${IfNot} ${RunningX64}
        MessageBox MB_ICONSTOP|MB_OK "DeskNotes requiere Windows de 64 bits."
        Abort
    ${EndIf}

    ; Buscar .NET 10 Desktop Runtime x64
    ; Directorio predeterminado de instalación

    FindFirst $0 $1 "$PROGRAMFILES64\dotnet\shared\Microsoft.WindowsDesktop.App\10.*"

    ${If} $1 == ""
        FindClose $0

        MessageBox MB_ICONEXCLAMATION|MB_YESNO \
        "DeskNotes necesita Microsoft .NET 10 Desktop Runtime x64.$\r$\n$\r$\nNo se ha encontrado una instalación compatible.$\r$\n$\r$\n¿Deseas abrir la página oficial para descargarlo?" \
        IDYES download_runtime IDNO cancel_install

        download_runtime:
            ExecShell "open" "https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
            MessageBox MB_OK|MB_ICONINFORMATION \
            "Instala .NET 10 Desktop Runtime x64 y vuelve a ejecutar el instalador de DeskNotes."
            Abort

        cancel_install:
            Abort
    ${EndIf}

    FindClose $0

FunctionEnd


; ============================================================
; INSTALACIÓN
; ============================================================

Section "DeskNotes" SEC_MAIN

    SetRegView 64
    SetShellVarContext all

    SetOutPath "$INSTDIR"

    File /r "${PUBLISH_DIR}\*.*"

    ; Accesos directos
    CreateDirectory "$SMPROGRAMS\DeskNotes"
    CreateShortcut "$SMPROGRAMS\DeskNotes\DeskNotes.lnk" "$INSTDIR\${APP_EXE}"
    CreateShortcut "$DESKTOP\DeskNotes.lnk" "$INSTDIR\${APP_EXE}"

    ; Desinstalador
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Registro de Windows
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "DisplayName" "${APP_NAME}"
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "Publisher" "${APP_PUBLISHER}"
    WriteRegStr HKLM "${APP_UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
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

    Delete "$INSTDIR\Uninstall.exe"
    RMDir /r "$INSTDIR"

    DeleteRegKey HKLM "${APP_UNINSTALL_KEY}"

SectionEnd
