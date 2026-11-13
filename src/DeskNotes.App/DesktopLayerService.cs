using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using DeskNotes.Core.Services;

namespace DeskNotes.App;

/// <summary>
/// Coordinates independent WPF popup HWNDs in the desktop Z-order band.
/// No widget is reparented to Explorer: each one remains a top-level WS_POPUP.
/// Explorer's desktop hierarchy is private implementation detail, so failures
/// leave widgets as ordinary windows instead of corrupting their HWND styles.
/// </summary>
internal sealed class DesktopLayerService : IDisposable
{
    private const uint DesktopWorkerMessage = 0x052C;
    private const uint SmtoNormal = 0x0000;
    private const uint SmtoAbortIfHung = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpNoSendChanging = 0x0400;
    private const uint PositionFlags = SwpNoSize | SwpNoMove | SwpNoActivate | SwpNoOwnerZOrder | SwpNoSendChanging;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsPopup = unchecked((long)0x80000000);
    private const long WsChild = 0x40000000;
    private const long WsExToolWindow = 0x00000080;
    private const long WsExNoRedirectionBitmap = 0x00200000;
    private const uint GaParent = 1;
    private const uint GaRoot = 2;
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutOfContext = 0x0000;
    private const uint WineventSkipOwnProcess = 0x0002;
    private static readonly IntPtr HwndBottom = new(1);
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNotTopmost = new(-2);

    private readonly Dictionary<IntPtr, Attachment> _widgets = [];
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _shellPoll;
    private readonly WinEventDelegate _winEventCallback;
    private IntPtr _helperWindow;
    private IntPtr _desktopHost;
    private IntPtr _shellView;
    private IntPtr _foregroundHook;
    private bool _desktopVisible;
    private bool _disposed;

    public DesktopLayerService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _winEventCallback = OnForegroundChanged;
        _shellPoll = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _shellPoll.Tick += (_, _) => RefreshDesktopState();
        _foregroundHook = SetWinEventHook(
            EventSystemForeground, EventSystemForeground, IntPtr.Zero,
            _winEventCallback, 0, 0,
            WineventOutOfContext | WineventSkipOwnProcess);
        _shellPoll.Start();
    }

    public bool TryAttach(Window window, out string error)
    {
        ArgumentNullException.ThrowIfNull(window);
        error = string.Empty;
        ObjectDisposedException.ThrowIf(_disposed, this);

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            error = "La ventana del widget todavía no tiene un identificador Win32.";
            return false;
        }
        if (_widgets.ContainsKey(hwnd)) return true;

        try
        {
            EnsureHelperWindow();
            var originalStyle = GetWindowLongPtr(hwnd, GwlStyle).ToInt64();
            if ((originalStyle & WsChild) != 0 || (originalStyle & WsPopup) == 0)
            {
                error = "El widget no conserva el estilo de ventana superior WS_POPUP.";
                return false;
            }

            // Keep the HWND top-level. Only adjust the tool-window hint; never call SetParent.
            var exStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            if ((exStyle & WsExToolWindow) == 0)
            {
                Marshal.SetLastPInvokeError(0);
                _ = SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(exStyle | WsExToolWindow));
                if (Marshal.GetLastPInvokeError() != 0)
                {
                    error = "No se pudo configurar el estilo auxiliar del widget.";
                    return false;
                }
            }

            _widgets.Add(hwnd, new Attachment(window, exStyle));
            RefreshDesktopState(force: true);
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or
                                   MarshalDirectiveException or InvalidOperationException)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Detach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !_widgets.Remove(hwnd, out var attachment)) return;

        // Return to the ordinary non-topmost window band without activating it.
        _ = SetWindowPos(hwnd, HwndNotTopmost, 0, 0, 0, 0, PositionFlags);
        _ = SetWindowPos(hwnd, HwndBottom, 0, 0, 0, 0, PositionFlags);
        _ = SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(attachment.OriginalExStyle));
        if (_widgets.Count == 0) _desktopVisible = false;
    }

    public void Forget(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero) _widgets.Remove(hwnd);
    }

    /// <summary>Explicit user action only. Automatic Z-order maintenance never activates a widget.</summary>
    public void Activate(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !_widgets.ContainsKey(hwnd)) return;
        window.Activate();
    }

    private void EnsureHelperWindow()
    {
        if (_helperWindow != IntPtr.Zero && IsWindow(_helperWindow)) return;
        _helperWindow = CreateWindowEx(
            0x00000080, "STATIC", "DeskNotes Desktop Layer",
            unchecked((int)(WsPopup | 0x08000000)), 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (_helperWindow == IntPtr.Zero)
            throw new InvalidOperationException("No se pudo crear la ventana auxiliar de orden Z.");
    }

    private void OnForegroundChanged(IntPtr hook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint threadId, uint eventTime)
    {
        if (_disposed) return;
        _ = _dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshDesktopState));
    }

    private void RefreshDesktopState(bool force = false)
    {
        if (_disposed || _widgets.Count == 0) return;

        var shellView = FindDesktopShellView();
        var host = shellView == IntPtr.Zero ? IntPtr.Zero : GetAncestor(shellView, GaRoot);
        if (host == IntPtr.Zero && shellView != IntPtr.Zero)
            host = GetAncestor(shellView, GaParent);

        // Explorer can restart and recreate every desktop HWND. Re-discover on each poll.
        var shellChanged = host != _desktopHost || shellView != _shellView;
        _desktopHost = host;
        _shellView = shellView;

        var foreground = GetForegroundWindow();
        var foregroundIsDesktop = foreground == host || foreground == GetAncestor(shellView, GaParent) ||
            IsForegroundDesktopWorker(foreground);
        var foregroundIsWidget = _widgets.ContainsKey(foreground);
        var desktopVisible = DesktopLayerStatePolicy.ShouldShowDesktopBand(
            host != IntPtr.Zero && IsWindowVisible(host),
            foregroundIsDesktop,
            foregroundIsWidget,
            _desktopVisible);

        if (!force && !shellChanged && desktopVisible == _desktopVisible)
        {
            // WPF may raise a clicked popup. Put it back behind the reference HWND
            // without changing activation or keyboard focus.
            if (foregroundIsWidget && _desktopVisible && _helperWindow != IntPtr.Zero)
                _ = SetWindowPos(foreground, _helperWindow, 0, 0, 0, 0, PositionFlags);
            return;
        }
        _desktopVisible = desktopVisible;

        if (_desktopVisible && _desktopHost != IntPtr.Zero)
            PositionHelperBehindDesktop(_desktopHost);
        else
            _ = SetWindowPos(_helperWindow, HwndBottom, 0, 0, 0, 0, PositionFlags);

        foreach (var hwnd in _widgets.Keys.ToArray())
        {
            if (!IsWindow(hwnd))
            {
                _widgets.Remove(hwnd);
                continue;
            }

            if (_desktopVisible)
            {
                // Insert each popup immediately behind the reference window.
                _ = SetWindowPos(hwnd, _helperWindow, 0, 0, 0, 0, PositionFlags);
            }
            else
            {
                // Keep widgets below normal application windows when the desktop is covered.
                _ = SetWindowPos(hwnd, HwndNotTopmost, 0, 0, 0, 0, PositionFlags);
                _ = SetWindowPos(hwnd, HwndBottom, 0, 0, 0, 0, PositionFlags);
            }
        }
    }

    private void PositionHelperBehindDesktop(IntPtr desktopHost)
    {
        _ = SetWindowPos(_helperWindow, HwndTopmost, 0, 0, 0, 0, PositionFlags);

        // Rainmeter's strategy: find the backmost topmost window above the desktop host,
        // then insert the helper immediately after it. This preserves the topmost band
        // without placing widgets above ordinary foreground applications.
        var candidate = desktopHost;
        while ((candidate = GetWindow(candidate, 3)) != IntPtr.Zero) // GW_HWNDPREV
        {
            if ((GetWindowLongPtr(candidate, GwlExStyle).ToInt64() & 0x00000008L) != 0)
            {
                if (SetWindowPos(_helperWindow, candidate, 0, 0, 0, 0, PositionFlags))
                    return;
            }
        }

        _ = SetWindowPos(_helperWindow, HwndTopmost, 0, 0, 0, 0, PositionFlags);
    }

    private static IntPtr FindDesktopShellView()
    {
        var progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            var view = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (view != IntPtr.Zero) return view;
        }

        IntPtr result = IntPtr.Zero;
        _ = EnumWindows((top, _) =>
        {
            result = FindWindowEx(top, IntPtr.Zero, "SHELLDLL_DefView", null);
            return result == IntPtr.Zero;
        }, IntPtr.Zero);
        return result;
    }

    private static bool IsForegroundDesktopWorker(IntPtr foreground)
    {
        if (foreground == IntPtr.Zero) return false;
        var className = new char[32];
        return GetClassName(foreground, className, className.Length) > 0 &&
               new string(className).TrimEnd('\0') == "WorkerW";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shellPoll.Stop();
        if (_foregroundHook != IntPtr.Zero)
        {
            _ = UnhookWinEvent(_foregroundHook);
            _foregroundHook = IntPtr.Zero;
        }

        foreach (var pair in _widgets.ToArray())
        {
            _ = SetWindowPos(pair.Key, HwndNotTopmost, 0, 0, 0, 0, PositionFlags);
            _ = SetWindowPos(pair.Key, HwndBottom, 0, 0, 0, 0, PositionFlags);
            _ = SetWindowLongPtr(pair.Key, GwlExStyle, new IntPtr(pair.Value.OriginalExStyle));
        }
        _widgets.Clear();

        if (_helperWindow != IntPtr.Zero && IsWindow(_helperWindow))
            _ = DestroyWindow(_helperWindow);
        _helperWindow = IntPtr.Zero;
    }

    private sealed record Attachment(Window Window, long OriginalExStyle);

    private delegate bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam);
    private delegate void WinEventDelegate(IntPtr hook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint threadId, uint eventTime);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string? windowName);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(IntPtr hwnd, char[] className, int maxCount);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint command);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName,
        int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y,
        int width, int height, uint flags);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module,
        WinEventDelegate callback, uint processId, uint threadId, uint flags);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWinEvent(IntPtr hook);
}
