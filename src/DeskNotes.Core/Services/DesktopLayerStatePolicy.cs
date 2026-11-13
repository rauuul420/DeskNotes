namespace DeskNotes.Core.Services;

/// <summary>
/// Pure policy for deciding whether desktop-layer windows belong in the visible
/// desktop Z-order band. Kept independent of Win32 so its focus transitions can
/// be verified by the cross-platform test suite.
/// </summary>
public static class DesktopLayerStatePolicy
{
    public static bool ShouldShowDesktopBand(
        bool desktopHostVisible,
        bool foregroundIsDesktop,
        bool foregroundIsManagedWidget,
        bool previousState)
    {
        // A widget receiving an explicit click is allowed to become foreground.
        // That must not be mistaken for leaving the desktop.
        if (foregroundIsManagedWidget)
        {
            return previousState;
        }

        return desktopHostVisible && foregroundIsDesktop;
    }
}
