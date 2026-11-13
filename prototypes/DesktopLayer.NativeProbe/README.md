# DeskNotes — Native Desktop Layer Probe

A standalone diagnostic prototype for Windows 11 25H2. It does not modify DeskNotes, user notes, or application settings.

## Build

Requires Windows and .NET 10 SDK:

\`\`\`powershell
dotnet build prototypes/DesktopLayer.NativeProbe/DesktopLayer.NativeProbe.csproj -c Release
dotnet run --project prototypes/DesktopLayer.NativeProbe/DesktopLayer.NativeProbe.csproj -c Release
\`\`\`

## Run

1. Start the probe.
2. Click **Mostrar superficie nativa**.
3. The control window minimizes and a semitransparent panel should appear on the desktop.
4. Confirm desktop icons remain visible and clickable above the panel.
5. Open a normal application and confirm it covers the panel.
6. Restore the probe from the taskbar and click **Detener superficie**.
7. If anything fails, send the diagnostic log.

Log location:

\`%LOCALAPPDATA%\\DeskNotes\\DesktopLayerPrototype\\diagnostics.log\`

The probe requires the modern Progman hierarchy and refuses to attach if it cannot validate the desktop view or match the DPI context. It does not attempt to repair or restart Explorer.

## Scope

This is Prototype 1 only. It validates a native layered child surface and Z-order. It does not validate WPF rendering, keyboard focus, multiple widgets, mixed DPI, or Explorer restart recovery.
