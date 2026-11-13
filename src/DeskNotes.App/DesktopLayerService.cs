using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DeskNotes.App;

/// <summary>
/// Hosts WPF widget HWNDs in the Explorer desktop surface, below the icon view.
/// Windows does not expose a supported public API for this scenario; the service
/// therefore detects the desktop composition and fails closed when it cannot
/// identify a safe parent window.
/// </summary>
internal sealed class DesktopLayerService
{
    private const uint DesktopWorkerMessage = 0x052C;
    private const uint SendMessageTimeoutFlags = 0x0002;
    private const uint WindowPositionFlags = 0x0010 | 0x0004 | 0x0040;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsChild = 0x40000000L;
    private const long WsPopup = unchecked((long)0x80000000L);
    private const long WsClipSiblings = 0x04000000L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoRedirectionBitmap = 0x00200000L;
    private static readonly IntPtr HwndBottom = new(1);
    private static readonly IntPtr HwndNotTopmost = new(-2);

    private readonly Dictionary<IntPtr, Attachment> _attachments = [];

    public bool TryAttach(Window window, out string error)
    {
        ArgumentNullException.ThrowIfNull(window);
        error = string.Empty;

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            error = "La ventana del widget todavía no tiene un identificador Win32.";
            return false;
        }

        if (_attachments.ContainsKey(hwnd))
        {
            return true;
        }

        try
        {
            var progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                error = "No se encontró la ventana Progman del escritorio.";
                return false;
            }

            _ = SendMessageTimeout(
                progman,
                DesktopWorkerMessage,
                new IntPtr(0xD),
                new IntPtr(1),
                SendMessageTimeoutFlags,
                1000,
                out _);

            var shellView = FindShellView(progman);
            if (shellView == IntPtr.Zero)
            {
                error = "No se encontró la superficie de iconos SHELLDLL_DefView.";
                return false;
            }

            var progmanExStyle = GetWindowLongPtr(progman, GwlExStyle).ToInt64();
            var isModernDesktop = (progmanExStyle & WsExNoRedirectionBitmap) != 0;
            var parent = isModernDesktop
                ? progman
                : FindLegacyWallpaperWorker(progman, shellView);

            if (parent == IntPtr.Zero)
            {
                error = isModernDesktop
                    ? "No se pudo utilizar Progman como contenedor de la capa moderna."
                    : "Explorer no creó una ventana WorkerW segura detrás de los iconos.";
                return false;
            }

            if (!GetWindowRect(hwnd, out var screenBounds))
            {
                error = "No se pudieron obtener los límites actuales del widget.";
                return false;
            }

            var originalStyle = GetWindowLongPtr(hwnd, GwlStyle);
            var originalExStyle = GetWindowLongPtr(hwnd, GwlExStyle);
            var originalParent = GetParent(hwnd);

            var childStyle = (originalStyle.ToInt64() & ~WsPopup) | WsChild | WsClipSiblings;
            var childExStyle = originalExStyle.ToInt64() | WsExToolWindow;

            Marshal.SetLastPInvokeError(0);
            _ = SetWindowLongPtr(hwnd, GwlStyle, new IntPtr(childStyle));
            if (Marshal.GetLastPInvokeError() != 0)
            {
                error = "No se pudieron actualizar los estilos Win32 del widget.";
                return false;
            }

            Marshal.SetLastPInvokeError(0);
            _ = SetParent(hwnd, parent);
            var parentError = Marshal.GetLastPInvokeError();
            if (parentError != 0)
            {
                _ = SetWindowLongPtr(hwnd, GwlStyle, originalStyle);
                error = $"Windows rechazó la integración del widget (código {parentError}).";
                return false;
            }

            _ = SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(childExStyle));

            var topLeft = new NativePoint { X = screenBounds.Left, Y = screenBounds.Top };
            if (!ScreenToClient(parent, ref topLeft))
            {
                _ = SetParent(hwnd, originalParent);
                _ = SetWindowLongPtr(hwnd, GwlStyle, originalStyle);
                _ = SetWindowLongPtr(hwnd, GwlExStyle, originalExStyle);
                error = "No se pudieron convertir las coordenadas del widget al escritorio.";
                return false;
            }

            var insertAfter = isModernDesktop ? shellView : HwndBottom;
            if (!SetWindowPos(
                    hwnd,
                    insertAfter,
                    topLeft.X,
                    topLeft.Y,
                    screenBounds.Right - screenBounds.Left,
                    screenBounds.Bottom - screenBounds.Top,
                    WindowPositionFlags))
            {
                _ = SetParent(hwnd, originalParent);
                _ = SetWindowLongPtr(hwnd, GwlStyle, originalStyle);
                _ = SetWindowLongPtr(hwnd, GwlExStyle, originalExStyle);
                error = "Windows rechazó el posicionamiento del widget en la capa del escritorio.";
                return false;
            }

            _attachments[hwnd] = new Attachment(originalParent, originalStyle, originalExStyle);
            return true;
        }
        catch (Exception exception) when (
            exception is DllNotFoundException or EntryPointNotFoundException or
            MarshalDirectiveException or InvalidOperationException)
        {
            error = exception.Message;
            return false;
        }
    }

    public void Detach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !_attachments.Remove(hwnd, out var attachment))
        {
            return;
        }

        if (!GetWindowRect(hwnd, out var currentBounds))
        {
            currentBounds = default;
        }

        Marshal.SetLastPInvokeError(0);
        _ = SetParent(hwnd, attachment.OriginalParent);
        if (Marshal.GetLastPInvokeError() != 0)
        {
            _attachments[hwnd] = attachment;
            return;
        }

        _ = SetWindowLongPtr(hwnd, GwlStyle, attachment.OriginalStyle);
        _ = SetWindowLongPtr(hwnd, GwlExStyle, attachment.OriginalExStyle);

        if (currentBounds.Right > currentBounds.Left && currentBounds.Bottom > currentBounds.Top)
        {
            _ = SetWindowPos(
                hwnd,
                HwndNotTopmost,
                currentBounds.Left,
                currentBounds.Top,
                currentBounds.Right - currentBounds.Left,
                currentBounds.Bottom - currentBounds.Top,
                WindowPositionFlags);
        }
    }

    /// <summary>Removes bookkeeping after WPF has destroyed the widget HWND.</summary>
    public void Forget(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero)
        {
            _attachments.Remove(hwnd);
        }
    }

    private static IntPtr FindShellView(IntPtr progman)
    {
        var shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (shellView != IntPtr.Zero)
        {
            return shellView;
        }

        IntPtr result = IntPtr.Zero;
        EnumWindows((topLevel, _) =>
        {
            result = FindWindowEx(topLevel, IntPtr.Zero, "SHELLDLL_DefView", null);
            return result == IntPtr.Zero;
        }, IntPtr.Zero);

        return result;
    }

    private static IntPtr FindLegacyWallpaperWorker(IntPtr progman, IntPtr shellView)
    {
        // Some Windows 11 desktop builds expose WorkerW as a child of Progman.
        var childWorker = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        if (childWorker != IntPtr.Zero)
        {
            return childWorker;
        }

        // Windows 10 commonly exposes the wallpaper WorkerW as a top-level
        // sibling immediately following the window that owns SHELLDLL_DefView.
        var iconHost = GetAncestor(shellView, 2); // GA_ROOT
        if (iconHost == IntPtr.Zero)
        {
            iconHost = progman;
        }

        return FindWindowEx(IntPtr.Zero, iconHost, "WorkerW", null);
    }

    private sealed record Attachment(
        IntPtr OriginalParent,
        IntPtr OriginalStyle,
        IntPtr OriginalExStyle);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    private delegate bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam,
        uint flags, uint timeout, out IntPtr result);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetParent(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ScreenToClient(IntPtr hwnd, ref NativePoint point);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
