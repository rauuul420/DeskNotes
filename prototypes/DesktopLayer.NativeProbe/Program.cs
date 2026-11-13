using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DeskNotes.DesktopLayer.NativeProbe;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ProbeLog.Initialize();
        ProbeLog.Write($"OS={Environment.OSVersion.Version}; Architecture={RuntimeInformation.ProcessArchitecture}; PointerSize={IntPtr.Size * 8}");

        var dpiReady = DesktopSurface.TryMatchExplorerDpi(out var dpiMessage);
        ProbeLog.Write(dpiMessage);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ProbeForm(dpiReady));
    }
}

internal sealed class ProbeForm : Form
{
    private readonly Button _startButton = new() { Text = "Mostrar superficie nativa", AutoSize = true };
    private readonly Button _stopButton = new() { Text = "Detener superficie", AutoSize = true, Enabled = false };
    private readonly TextBox _logBox = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill
    };

    private DesktopSurface? _surface;

    public ProbeForm(bool dpiReady)
    {
        Text = "DeskNotes — Native Desktop Layer Probe";
        Width = 760;
        Height = 460;
        StartPosition = FormStartPosition.CenterScreen;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        buttons.Controls.Add(_startButton);
        buttons.Controls.Add(_stopButton);

        Controls.Add(_logBox);
        Controls.Add(buttons);

        _startButton.Enabled = dpiReady;
        _startButton.Click += (_, _) => StartSurface();
        _stopButton.Click += (_, _) => StopSurface();

        FormClosed += (_, _) => StopSurface();
        ProbeLog.MessageWritten += AppendLog;
        AppendLog($"Registro: {ProbeLog.FilePath}");
        AppendLog(dpiReady ? "DPI: contexto compatible con Explorer." : "DPI: no se pudo garantizar compatibilidad; creación deshabilitada.");
    }

    private void StartSurface()
    {
        try
        {
            _surface = DesktopSurface.Create(ProbeLog.Write);
            _startButton.Enabled = false;
            _stopButton.Enabled = true;
            ProbeLog.Write("SUPERFICIE CREADA. La ventana de control se minimizará.");
            WindowState = FormWindowState.Minimized;
        }
        catch (Exception ex)
        {
            ProbeLog.Write($"ERROR: {ex}");
            MessageBox.Show(this, ex.Message, "No se pudo crear la superficie", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StopSurface()
    {
        _surface?.Dispose();
        _surface = null;
        if (!IsDisposed)
        {
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
        }
        ProbeLog.Write("Superficie detenida.");
    }

    private void AppendLog(string message)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        if (InvokeRequired)
        {
            BeginInvoke((Action)(() => AppendLog(message)));
            return;
        }

        _logBox.AppendText(message + Environment.NewLine);
    }
}

internal sealed class DesktopSurface : IDisposable
{
    private const int Width = 440;
    private const int Height = 230;
    private const int GwlExStyle = -20;
    private const long WsExLayered = 0x00080000L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const long WsExNoRedirectionBitmap = 0x00200000L;
    private const uint WsChild = 0x40000000;
    private const uint WsVisible = 0x10000000;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const uint UlwAlpha = 0x00000002;
    private const byte AcSrcOver = 0;
    private const byte AcSrcAlpha = 1;
    private const uint DesktopWorkerMessage = 0x052C;
    private const uint SmtoAbortIfHung = 0x0002;
    private const uint DibRgbColors = 0;
    private const uint BiRgb = 0;
    private const uint GwHwndNext = 2;
    private const uint GwHwndPrev = 3;
    private const uint ErrorClassAlreadyExists = 1410;

    private static readonly WndProc WindowProcedure = NativeDefWindowProc;
    private static readonly string WindowClassName = "DeskNotes.NativeDesktopProbe.Window";
    private readonly Action<string> _log;
    private readonly IntPtr _hwnd;
    private readonly IntPtr _memoryDc;
    private readonly IntPtr _bitmap;
    private readonly IntPtr _previousBitmap;
    private bool _disposed;

    private DesktopSurface(Action<string> log, IntPtr hwnd, IntPtr memoryDc, IntPtr bitmap, IntPtr previousBitmap)
    {
        _log = log;
        _hwnd = hwnd;
        _memoryDc = memoryDc;
        _bitmap = bitmap;
        _previousBitmap = previousBitmap;
    }

    public static DesktopSurface Create(Action<string> log)
    {
        ArgumentNullException.ThrowIfNull(log);

        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
            throw NativeFailure("No se encontró Progman.");

        _ = SendMessageTimeout(progman, DesktopWorkerMessage,
            new IntPtr(0x0D), new IntPtr(0x01), SmtoAbortIfHung, 1000, out _);

        var progmanExStyle = GetWindowLongPtr(progman, GwlExStyle).ToInt64();
        if ((progmanExStyle & WsExNoRedirectionBitmap) == 0)
            throw NativeFailure("Progman no presenta WS_EX_NOREDIRECTIONBITMAP. No se ha demostrado la ruta moderna esperada.");

        var shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (shellView == IntPtr.Zero || GetParent(shellView) != progman)
            throw NativeFailure("SHELLDLL_DefView no es un hijo directo verificable de Progman.");

        var shellDpi = GetWindowDpiAwarenessContext(progman);
        var threadDpi = GetThreadDpiAwarenessContext();
        if (!AreDpiAwarenessContextsEqual(shellDpi, threadDpi))
            throw NativeFailure("El contexto DPI del hilo ya no coincide con Progman. Se aborta para evitar un reset de DPI del proceso.");

        var client = new RECT();
        if (!GetClientRect(progman, out client))
            throw NativeFailure("GetClientRect(Progman) falló.");

        var clientWidth = client.Right - client.Left;
        var clientHeight = client.Bottom - client.Top;
        if (clientWidth < Width || clientHeight < Height)
            throw NativeFailure($"Área cliente de Progman inesperada: {clientWidth}x{clientHeight}.");

        RegisterWindowClass();
        var instance = GetModuleHandle(null);
        var hwnd = CreateWindowEx(
            WsExLayered | WsExToolWindow | WsExNoActivate,
            WindowClassName,
            "DeskNotes native desktop probe",
            WsChild | WsVisible,
            0, 0, Width, Height,
            progman, IntPtr.Zero, instance, IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
            throw NativeFailure("CreateWindowEx(WS_CHILD | WS_EX_LAYERED) falló.");

        IntPtr memoryDc = IntPtr.Zero;
        IntPtr bitmap = IntPtr.Zero;
        IntPtr previousBitmap = IntPtr.Zero;

        try
        {
            var x = Math.Max(0, (clientWidth - Width) / 2);
            var y = Math.Max(0, (clientHeight - Height) / 2);

            if (!SetWindowPos(hwnd, shellView, x, y, Width, Height, SwpNoActivate | SwpShowWindow))
                throw NativeFailure("SetWindowPos detrás de SHELLDLL_DefView falló.");

            memoryDc = CreateCompatibleDC(IntPtr.Zero);
            if (memoryDc == IntPtr.Zero)
                throw NativeFailure("CreateCompatibleDC falló.");

            var bitmapInfo = new BITMAPINFO
            {
                Header = new BITMAPINFOHEADER
                {
                    Size = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                    Width = Width,
                    Height = -Height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = BiRgb,
                    SizeImage = (uint)(Width * Height * 4)
                }
            };

            bitmap = CreateDIBSection(IntPtr.Zero, ref bitmapInfo, DibRgbColors, out var bits, IntPtr.Zero, 0);
            if (bitmap == IntPtr.Zero || bits == IntPtr.Zero)
                throw NativeFailure("CreateDIBSection falló.");

            previousBitmap = SelectObject(memoryDc, bitmap);
            if (previousBitmap == IntPtr.Zero || previousBitmap == new IntPtr(-1))
                throw NativeFailure("SelectObject falló.");

            var pixels = CreatePremultipliedPanel();
            Marshal.Copy(pixels, 0, bits, pixels.Length);

            var size = new SIZE { Width = Width, Height = Height };
            var sourcePoint = new POINT { X = 0, Y = 0 };
            var blend = new BLENDFUNCTION
            {
                BlendOp = AcSrcOver,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = AcSrcAlpha
            };

            if (!UpdateLayeredWindow(hwnd, IntPtr.Zero, IntPtr.Zero, ref size,
                    memoryDc, ref sourcePoint, 0, ref blend, UlwAlpha))
                throw NativeFailure("UpdateLayeredWindow(ULW_ALPHA) falló.");

            var parent = GetParent(hwnd);
            var previous = GetWindow(hwnd, GwHwndPrev);
            var next = GetWindow(hwnd, GwHwndNext);
            var rect = new RECT();
            _ = GetWindowRect(hwnd, out rect);

            log($"Progman={Format(progman)}; ShellView={Format(shellView)}; Widget={Format(hwnd)}");
            log($"Parent(widget)={Format(parent)}; previousZ={Format(previous)}; nextZ={Format(next)}");
            log($"Parent(widget)==Progman: {parent == progman}; previousZ==ShellView: {previous == shellView}");
            log($"Progman exStyle=0x{progmanExStyle:X}; widget exStyle=0x{GetWindowLongPtr(hwnd, GwlExStyle).ToInt64():X}");
            log($"Progman client={clientWidth}x{clientHeight}; widget rect=({rect.Left},{rect.Top},{rect.Right},{rect.Bottom})");
            log($"DPI Progman=0x{shellDpi.ToInt64():X}; DPI thread=0x{threadDpi.ToInt64():X}; equal={AreDpiAwarenessContextsEqual(shellDpi, threadDpi)}");
            log("No se ha cambiado el Z-order de WorkerW.");

            return new DesktopSurface(log, hwnd, memoryDc, bitmap, previousBitmap);
        }
        catch
        {
            if (memoryDc != IntPtr.Zero && previousBitmap != IntPtr.Zero)
                _ = SelectObject(memoryDc, previousBitmap);
            if (bitmap != IntPtr.Zero)
                _ = DeleteObject(bitmap);
            if (memoryDc != IntPtr.Zero)
                _ = DeleteDC(memoryDc);
            _ = DestroyWindow(hwnd);
            throw;
        }
    }

    private static byte[] CreatePremultipliedPanel()
    {
        var pixels = new byte[Width * Height * 4];

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var border = x < 3 || y < 3 || x >= Width - 3 || y >= Height - 3;
                var alpha = border ? 255 : (byte)218;
                var red = border ? (byte)95 : (byte)28;
                var green = border ? (byte)165 : (byte)38;
                var blue = border ? (byte)245 : (byte)58;
                var index = (y * Width + x) * 4;

                // UpdateLayeredWindow expects premultiplied BGRA when AC_SRC_ALPHA is used.
                pixels[index] = (byte)(blue * alpha / 255);
                pixels[index + 1] = (byte)(green * alpha / 255);
                pixels[index + 2] = (byte)(red * alpha / 255);
                pixels[index + 3] = alpha;
            }
        }

        return pixels;
    }

    private static void RegisterWindowClass()
    {
        var windowClass = new WNDCLASSEX
        {
            Size = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            WindowProc = WindowProcedure,
            Instance = GetModuleHandle(null),
            ClassName = WindowClassName
        };

        if (RegisterClassEx(ref windowClass) == 0)
        {
            var error = Marshal.GetLastWin32Error();
            if (error != ErrorClassAlreadyExists)
                throw new Win32Exception(error, "RegisterClassEx falló.");
        }
    }

    private static Exception NativeFailure(string message)
    {
        var error = Marshal.GetLastWin32Error();
        return new Win32Exception(error, $"{message} Win32Error={error}.");
    }

    private static string Format(IntPtr hwnd) => $"0x{hwnd.ToInt64():X}";

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (IsWindow(_hwnd))
            _ = DestroyWindow(_hwnd);

        if (_memoryDc != IntPtr.Zero && _previousBitmap != IntPtr.Zero)
            _ = SelectObject(_memoryDc, _previousBitmap);
        if (_bitmap != IntPtr.Zero)
            _ = DeleteObject(_bitmap);
        if (_memoryDc != IntPtr.Zero)
            _ = DeleteDC(_memoryDc);

        _log("HWND nativo destruido y recursos GDI liberados.");
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint Size;
        public uint Style;
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public WndProc? WindowProc;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public string? MenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string ClassName;
        public IntPtr SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE { public int Width, Height; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER Header;
        public uint Color;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetParent(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetThreadDpiAwarenessContext();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr value);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(long extendedStyle, string className, string windowName,
        uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW", SetLastError = true)]
    private static extern IntPtr NativeDefWindowProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bitmapInfo, uint usage,
        out IntPtr bits, IntPtr section, uint offset);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr destinationDc, IntPtr destinationPoint,
        ref SIZE size, IntPtr sourceDc, ref POINT sourcePoint, uint colorKey, ref BLENDFUNCTION blend, uint flags);

    internal static bool TryMatchExplorerDpi(out string message)
    {
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            message = "No se encontró Progman al configurar DPI.";
            return false;
        }

        var shellContext = GetWindowDpiAwarenessContext(progman);
        if (shellContext == IntPtr.Zero)
        {
            message = "GetWindowDpiAwarenessContext(Progman) devolvió NULL.";
            return false;
        }

        var currentContext = GetThreadDpiAwarenessContext();
        var processSet = false;
        if (!AreDpiAwarenessContextsEqual(shellContext, currentContext))
            processSet = SetProcessDpiAwarenessContext(shellContext);

        currentContext = GetThreadDpiAwarenessContext();
        if (!AreDpiAwarenessContextsEqual(shellContext, currentContext))
        {
            _ = SetThreadDpiAwarenessContext(shellContext);
            currentContext = GetThreadDpiAwarenessContext();
        }

        var equal = AreDpiAwarenessContextsEqual(shellContext, currentContext);
        var shellAwareness = GetAwarenessFromDpiAwarenessContext(shellContext);
        var threadAwareness = GetAwarenessFromDpiAwarenessContext(currentContext);
        message = $"DPI setup: processSet={processSet}; ProgmanContext=0x{shellContext.ToInt64():X} (awareness={shellAwareness}); ThreadContext=0x{currentContext.ToInt64():X} (awareness={threadAwareness}); equal={equal}; SetProcessError={Marshal.GetLastWin32Error()}";
        ProbeLog.Write(message);
        return equal;
    }
}

internal static class ProbeLog
{
    private static readonly object Sync = new();

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DeskNotes", "DesktopLayerPrototype", "diagnostics.log");

    public static event Action<string>? MessageWritten;

    public static void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, $"DeskNotes native desktop probe — {DateTimeOffset.Now:O}{Environment.NewLine}");
    }

    public static void Write(string message)
    {
        var line = $"{DateTimeOffset.Now:O} {message}";
        lock (Sync)
            File.AppendAllText(FilePath, line + Environment.NewLine);
        MessageWritten?.Invoke(line);
    }
}
