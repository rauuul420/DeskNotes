# Desktop widget layer

DeskNotes can optionally host note widgets between the wallpaper surface and the desktop icon view.

## Windows desktop variants

- Windows 10 and classic desktop composition: request the Explorer wallpaper surface and attach the widget HWND to the WorkerW behind the icon host.
- Windows 11 builds exposing the classic WorkerW hierarchy: prefer the WorkerW child of Progman, then use the classic sibling lookup.
- Windows 11 24H2 and later desktop composition: detect `WS_EX_NOREDIRECTIONBITMAP` on Progman and attach to Progman, placing the widget immediately behind `SHELLDLL_DefView`.

This uses undocumented Explorer behavior (including message `0x052C`). Microsoft does not provide a public compatibility contract for third-party windows in this layer. Explorer updates can change the hierarchy; therefore DeskNotes fails closed and leaves widgets as ordinary windows when it cannot find a suitable host.

## Interaction and constraints

Widgets remain WPF windows and retain their normal content, drag and resize behavior while hosted. The topmost/pin action is temporarily unavailable in desktop-layer mode because a topmost window cannot simultaneously remain below the desktop icon view. Its previous value is restored when desktop-layer mode is disabled.

The setting is opt-in and stored in `%LocalAppData%/DeskNotes/settings.json`. It applies to existing widgets and to widgets created later. If attaching any widget fails during activation, DeskNotes detaches all widgets, disables the setting and reports the reason.

## Required manual validation

CI compilation is not a substitute for Explorer desktop validation. Before release, test on physical Windows 10 and Windows 11 machines, including Windows 11 24H2 or later:

1. Enable and disable the option with multiple widgets open.
2. Verify icons remain visible and clickable above the widgets.
3. Click widget buttons; drag and resize widgets; verify text rendering and transparency.
4. Switch monitors/resolutions and test widgets on negative virtual-screen coordinates.
5. Restart Explorer and DeskNotes; verify normal fallback and persisted preference behavior.
6. Confirm ordinary application windows always cover desktop-layer widgets.
7. Verify normal mode and the per-widget topmost action still work after disabling the feature.
