# Desktop widget layer

DeskNotes provides an experimental desktop layer for placing interactive note widgets between the wallpaper and the desktop icons.

## Windows 11 24H2 / 25H2 implementation

The first supported path targets Windows 11 build 26100 or later (24H2 and 25H2). It uses the modern Explorer desktop hierarchy observed on these releases:

- Locate the top-level `Progman` window.
- Ask Explorer to initialize its desktop WorkerW band with message `0x052C` (modern parameters first).
- Require `SHELLDLL_DefView` to be a direct child of `Progman`.
- Locate the wallpaper `WorkerW` as a direct child of `Progman`, when present.
- Convert each WPF widget HWND from a top-level popup to a child window and parent it directly to `Progman`.
- Place the widget immediately below `SHELLDLL_DefView`, then place the wallpaper `WorkerW` below the widget.

This is a reverse-engineered Explorer integration, not a supported Windows API. The feature must remain opt-in and experimental.

## WPF rendering and interaction

- Widget windows use `AllowsTransparency=True`, so WPF already owns the layered-window rendering path.
- The service deliberately does not call `SetLayeredWindowAttributes`: doing so can conflict with WPF's per-pixel layered rendering.
- The implementation preserves the original style, extended style and parent, and restores them when the feature is disabled or the application exits.
- The widget remains a WPF window, but changing its HWND into a child window can affect WPF activation, keyboard focus, drag/resize and input routing. These behaviors require hands-on validation on Windows 11 25H2 before merging.

## Compatibility scope

- Windows 11 24H2 / 25H2: modern `Progman` path (build 26100+), experimental.
- Earlier Windows versions: deliberately not enabled yet. A separate legacy `WorkerW` strategy can be added after the modern path is validated.
- If the expected Explorer hierarchy is not found, activation fails closed and widgets remain ordinary windows.
- Explorer's hierarchy and message `0x052C` are undocumented implementation details and may change in future cumulative updates.

## Manual validation required on Windows 11 25H2

1. Enable and disable the option with one and multiple widgets.
2. Confirm icons remain visible, clickable and above the widgets.
3. Confirm widget buttons, double-click-to-edit, scrolling, drag and resize work.
4. Confirm keyboard focus enters the widget and does not get trapped or lost.
5. Open normal applications and verify they cover the widgets.
6. Verify the widgets do not appear in Alt+Tab or the taskbar unexpectedly.
7. Restart Explorer while enabled and verify no orphaned or destroyed widget windows remain.
8. Verify disabling restores normal top-level windows and the previous topmost setting.
9. Test mixed-DPI and multiple-monitor arrangements, including monitors left/above the primary display.

Automated unit tests cannot validate Explorer's native Z-order, DWM composition or WPF child-window input behavior. Do not merge or publish this feature as stable until the manual checks pass.
