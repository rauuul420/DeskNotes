using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DeskNotes.App;

/// <summary>
/// Hosts WPF widget HWNDs as child windows of the modern Windows desktop root.
/// Windows 11 24H2/25H2 uses the Progman composition path: widgets are siblings
/// of SHELLDLL_DefView and are ordered above the wallpaper WorkerW.
/// This relies on undocumented Explorer implementation details.
/// </summary>
internal sealed class DesktopLayerService : IDisposable
{
    private const int MinimumModernDesktopBuild = 26100;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsChild = 0x40000000;
    private const long WsPopup = unchecked((long)0x80000000);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint PositionFlags = SwpNoSize | SwpNoMove | SwpNoActivate | SwpFrameChanged;
    private const uint SmtoAbortIfHung = 0x0002;
    private const uint GaParent = 1;
    private const uint DesktopWorkerMessage = 0x052C;
    private static readonly nuint ModernWorkerWParam = 0x000D;
    private static readonly nint ModernWorkerLParam = 0x0001;

    private readonly Dictionary<IntPtr, Attachment> _widgets = [];
    private readonly DispatcherTimer _shellPoll;
    private IntPtr _progman;
    private IntPtr _shellView;
    private IntPtr _workerW;
    private bool _disposed;

    public DesktopLayerService(Dispatcher dispatcher)
    {
        _shellPoll = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _shellPoll.Tick += (_, _) => RefreshShellHandles();
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
            error = "El widget todavía no tiene un identificador Win32.";
            return false;
        }
        if (_widgets.ContainsKey(hwnd))
            return true;

        try
        {
            if (!IsSupportedModernWindows())
            {
                error = "Esta primera implementación requiere Windows 11 24H2 o 25H2 (compilación 26100 o posterior).";
                return false;
            }

            if (!DiscoverDesktopWindows(out error))
                return false;

            var style = GetWindowLongPtr(hwnd, GwlStyle).ToInt64();
            if ((style & WsChild) != 0)
            {
                error = "El HWND del widget ya es una ventana hija y no se puede adjuntar de forma segura.";
                return false;
            }

            var exStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            var originalParent = GetParent(hwnd);
            var attachment = new Attachment(window, originalParent, style, exStyle);

            // WPF widgets use AllowsTransparency=True, which already enables
            // WS_EX_LAYERED. Do not call SetLayeredWindowAttributes here:
            // it would interfere with WPF's per-pixel UpdateLayeredWindow path.
            var childStyle = (style & ~WsPopup) | WsChild;
            SetWindowLongChecked(hwnd, GwlStyle, childStyle);

            Marshal.SetLastPInvokeError(0);
            var previousParent = SetParent(hwnd, _progman);
            var parentError = Marshal.GetLastPInvokeError();
            if (previousParent == IntPtr.Zero && parentError != 0)
            {
                SetWindowLongPtr(hwnd, GwlStyle, new IntPtr(style));
                error = $"SetParent(Progman) falló. Error Win32: {parentError}.";
                return false;
            }

            _widgets.Add(hwnd, attachment);

            // SetWindowPos(hwnd, shellView) places the widget immediately below
            // the icon view. Then put WorkerW below the widget, so the wallpaper
            // remains behind it. All are children of Progman on the modern path.
            if (!SetWindowPos(hwnd, _shellView, 0, 0, 0, 0, PositionFlags) ||
                (_workerW != IntPtr.Zero &&
                 !SetWindowPos(_workerW, hwnd, 0, 0, 0, 0, PositionFlags)))
            {
                var win32Error = Marshal.GetLastPInvokeError();
                RestoreAttachment(hwnd, attachment);
                _widgets.Remove(hwnd);
                error = $"No se pudo establecer el orden Z de Progman. Error Win32: {win32Error}.";
                return false;
            }

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
        if (hwnd == IntPtr.Zero || !_widgets.Remove(hwnd, out var attachment))
            return;

        RestoreAttachment(hwnd, attachment);
    }

    public void Forget(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero)
            _widgets.Remove(hwnd);
    }

    private bool DiscoverDesktopWindows(out string error)
    {
        error = string.Empty;
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            error = "No se encontró la ventana Progman de Explorer.";
            return false;
        }

        // On Windows 11 24H2/25H2 Explorer exposes the icon view and wallpaper
        // WorkerW as children of Progman. Ask the shell to initialize that band
        // using the modern message parameters, then query the hierarchy again.
        _ = SendMessageTimeout(progman, DesktopWorkerMessage,
            ModernWorkerWParam, ModernWorkerLParam,
            SmtoAbortIfHung, 1000, out _);

        var shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (shellView == IntPtr.Zero)
        {
            error = "Explorer no expone SHELLDLL_DefView como hijo directo de Progman. No se modificará ningún widget.";
            return false;
        }

        var workerW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        if (workerW == IntPtr.Zero)
        {
            // Some builds create the wallpaper WorkerW only after the legacy
            // initialization message. Re-query once; never fall back to guessing
            // an unrelated top-level WorkerW.
            _ = SendMessageTimeout(progman, DesktopWorkerMessage,
                0, 0, SmtoAbortIfHung, 1000, out _);
            workerW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        }

        _progman = progman;
        _shellView = shellView;
        _workerW = workerW;
        return true;
    }

    private void RefreshShellHandles()
    {
        if (_disposed || _widgets.Count == 0)
            return;

        if (_progman == IntPtr.Zero || !IsWindow(_progman) ||
            _shellView == IntPtr.Zero || !IsWindow(_shellView))
        {
            // Explorer may have restarted. Do not silently reparent HWNDs to an
            // unverified window tree; let the user return to normal mode instead.
            return;
        }
    }

    private static bool IsSupportedModernWindows()
    {
        var version = new OsVersionInfo { Size = (uint)Marshal.SizeOf<OsVersionInfo>() };
        return RtlGetVersion(ref version) == 0 &&
               version.MajorVersion == 10 &&
               version.BuildNumber >= MinimumModernDesktopBuild;
    }

    private static void SetWindowLongChecked(IntPtr hwnd, int index, long value)
    {
        Marshal.SetLastPInvokeError(0);
        _ = SetWindowLongPtr(hwnd, index, new IntPtr(value));
        var error = Marshal.GetLastPInvokeError();
        if (error != 0)
            throw new InvalidOperationException($"SetWindowLongPtr falló. Error Win32: {error}.");
    }

    private static void RestoreAttachment(IntPtr hwnd, Attachment attachment)
    {
        if (!IsWindow(hwnd))
            return;

        Marshal.SetLastPInvokeError(0);
        _ = SetParent(hwnd, attachment.OriginalParent);
        var parentError = Marshal.GetLastPInvokeError();

        _ = SetWindowLongPtr(hwnd, GwlStyle, new IntPtr(attachment.OriginalStyle));
        _ = SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(attachment.OriginalExStyle));
        _ = SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpFrameChanged);

        if (parentError != 0)
            System.Diagnostics.Debug.WriteLine($"DeskNotes: no se pudo restaurar el parent del widget. Error Win32: {parentError}.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _shellPoll.Stop();

        foreach (var pair in _widgets.ToArray())
            RestoreAttachment(pair.Key, pair.Value);

        _widgets.Clear();
    }

    private sealed record Attachment(
        Window Window,
        IntPtr OriginalParent,
        long OriginalStyle,
        long OriginalExStyle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OsVersionInfo
    {
        public uint Size;
        public uint MajorVersion;
        public uint MinorVersion;
        public uint BuildNumber;
        public uint PlatformId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? ServicePack;
    }

    [DllImport("ntdll.dll")]
    private static extern int RtlGetVersion(ref OsVersionInfo version);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetParent(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y,
        int width, int height, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message,
        nuint wParam, nint lParam, uint flags, uint timeout, out nuint result);
}
