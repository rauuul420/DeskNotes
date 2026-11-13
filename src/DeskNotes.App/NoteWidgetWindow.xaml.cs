using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeskNotes.Core.Models;

namespace DeskNotes.App;

/// <summary>
/// A compact, borderless desktop widget that previews a saved note.
/// </summary>
public partial class NoteWidgetWindow : Window
{
    private readonly string _filePath;

    public event EventHandler? PlacementChanged;

    public event Action<NoteWidgetWindow>? EditRequested;

    public string FilePath => _filePath;

    public NoteWidgetWindow(
        string filePath,
        string title,
        string content,
        NoteWidgetState? savedState = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        InitializeComponent();

        _filePath = Path.GetFullPath(filePath);
        UpdateContent(title, content);

        if (savedState is not null && IsValidVisiblePlacement(savedState))
        {
            Left = savedState.Left;
            Top = savedState.Top;
            Width = savedState.Width;
            Height = savedState.Height;
            Topmost = savedState.IsTopmost;
        }
        else
        {
            Left = Math.Max(SystemParameters.WorkArea.Left + 12,
                SystemParameters.WorkArea.Right - Width - 24);
            Top = SystemParameters.WorkArea.Top + 48;
        }

        UpdatePinButton();

        LocationChanged += OnPlacementChanged;
        SizeChanged += OnPlacementChanged;
    }

    public void UpdateContent(string title, string content)
    {
        WidgetTitleText.Text = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(_filePath)
            : title;
        WidgetContentText.Text = content ?? string.Empty;
    }

    public NoteWidgetState? CaptureState()
    {
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        if (bounds.Width < 220 || bounds.Height < 150)
        {
            return null;
        }

        return new NoteWidgetState
        {
            FilePath = _filePath,
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            IsTopmost = Topmost
        };
    }

    private static bool IsValidVisiblePlacement(NoteWidgetState state)
    {
        if (!double.IsFinite(state.Left) ||
            !double.IsFinite(state.Top) ||
            !double.IsFinite(state.Width) ||
            !double.IsFinite(state.Height) ||
            state.Width < 220 ||
            state.Height < 150)
        {
            return false;
        }

        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        var widgetBounds = new Rect(state.Left, state.Top, state.Width, state.Height);
        var visibleBounds = Rect.Intersect(virtualScreen, widgetBounds);

        return !visibleBounds.IsEmpty &&
               visibleBounds.Width >= 100 &&
               visibleBounds.Height >= 80;
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        PlacementChanged?.Invoke(this, EventArgs.Empty);
    }

    private void WidgetWindow_Loaded(object sender, RoutedEventArgs e)
    {
        OnPlacementChanged(this, EventArgs.Empty);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            EditRequested?.Invoke(this);
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        UpdatePinButton();
        OnPlacementChanged(this, EventArgs.Empty);
    }

    private void UpdatePinButton()
    {
        PinButton.Content = Topmost ? "◆" : "◇";
        PinButton.ToolTip = Topmost
            ? "Desactivar mantener encima"
            : "Mantener encima de otras ventanas";
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        EditRequested?.Invoke(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void WidgetWindow_Closing(object? sender, CancelEventArgs e)
    {
        PlacementChanged?.Invoke(this, EventArgs.Empty);
    }
}
