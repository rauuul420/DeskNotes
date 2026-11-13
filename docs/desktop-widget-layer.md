# Desktop widget layer

DeskNotes optionally positions note widgets in the desktop Z-order band between the wallpaper and desktop icons.

## Window architecture

- Each widget remains an independent top-level WPF window with its original native window style. The implementation verifies that its HWND is not `WS_CHILD` and is its own `GA_ROOT`; it does not assume that WPF must use `WS_POPUP`.
- Widgets are never reparented to `Progman` or `WorkerW`; the service does not call `SetParent` and does not add `WS_CHILD`.
- A hidden, disabled helper popup provides the reference point for Z-order placement, following the strategy used by Rainmeter.
- Automatic Z-order changes use `SetWindowPos` with `SWP_NOACTIVATE`; they must not steal keyboard focus.
- A foreground WinEvent hook and a lightweight dispatcher poll detect desktop transitions and Explorer HWND recreation. The service rediscovers `SHELLDLL_DefView` and its root host after Explorer restarts.

## Windows compatibility and limitations

Explorer's desktop window hierarchy and message `0x052C` are implementation details, not a supported public Windows API. Windows 10 and Windows 11 builds may expose different host hierarchies. The service fails closed when it cannot identify a desktop host; it does not attempt to mutate widget parentage or styles to force an attachment.

The helper-window strategy follows Rainmeter's public source implementation, but DeskNotes is a WPF application and still requires validation on actual Windows installations. The current implementation is not declared compatible with every Windows 10/11 build until the manual matrix below passes.

## Interaction and focus

- A user click may explicitly activate a widget; automatic maintenance never calls `Activate`, `BringWindowToTop` or `SetForegroundWindow`.
- A foreground event caused by one of DeskNotes' own widgets preserves the current desktop-band state instead of interpreting it as the desktop being covered.
- The pin/topmost action is temporarily unavailable while desktop-layer mode is enabled. Its previous value is restored when the option is disabled.
- Disabling the feature returns each widget to the ordinary non-topmost window band and restores its original extended style.

## Recovery and monitor changes

The service periodically rediscovers Explorer's desktop host. If Explorer restarts and recreates its HWNDs, the helper is repositioned against the new host. Widget HWNDs remain top-level and retain their screen coordinates, so monitor topology changes do not require parent-coordinate conversion. The desktop poll also re-evaluates the current host after display changes.

## Automated and manual validation

The cross-platform tests cover the pure foreground/focus state policy. They do not simulate Explorer or prove native Z-order behavior.

Before merging or releasing, test on physical Windows 10 and Windows 11 systems, including Windows 11 24H2 or later:

1. Enable and disable the option with multiple widgets open.
2. Verify desktop icons remain visible and clickable above widgets.
3. Click widget controls; drag and resize; verify text rendering and transparency.
4. Click a widget while the desktop is visible and confirm it remains visible and receives keyboard focus where applicable.
5. Switch monitors/resolutions, including negative virtual-screen coordinates.
6. Restart Explorer while the option is enabled and verify recovery without orphaned or hidden widgets.
7. Open and switch between ordinary applications; verify they cover desktop-layer widgets and automatic Z-order maintenance never steals focus.
8. Verify normal mode and the per-widget topmost action work after disabling the feature.
