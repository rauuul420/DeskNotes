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

The probe requires the raised-desktop Progman hierarchy and refuses to attach if it cannot validate the desktop view, a direct Progman WorkerW child, or the DPI context. It places the surface behind SHELLDLL_DefView and moves WorkerW to the bottom of Progman's child Z-order.

Two alpha paths are tested in separate process runs:

- Default: UpdateLayeredWindow with per-pixel alpha.
- `--constant-alpha`: SetLayeredWindowAttributes(alpha=255), with ordinary GDI painting, matching the alpha strategy documented in Lively's DirectX path.

The executable embeds a compatibility manifest declaring Windows 8, 8.1 and Windows 10+ support. Microsoft requires Windows 8+ manifest compatibility for layered child windows. This prototype targets Windows 11 25H2; the manifest does not imply that earlier Windows versions have been tested.

It does not attempt to repair or restart Explorer.

## Scope

This is Prototype 1 only. It validates a native layered child surface, both alpha paths, and the Progman / SHELLDLL_DefView / WorkerW Z-order. It does not validate WPF rendering, keyboard focus, multiple widgets, mixed DPI, or Explorer restart recovery.
