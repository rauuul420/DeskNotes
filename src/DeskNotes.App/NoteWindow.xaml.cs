using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeskNotes.Core.Interfaces;
using DeskNotes.Core.Models;
using Microsoft.Win32;

namespace DeskNotes.App;

/// <summary>
/// An independent editor window for one TXT or Markdown note.
/// </summary>
public partial class NoteWindow : Window
{
    private readonly Note _note;
    private readonly INoteFileService _noteFileService;
    private string? _filePath;
    private bool _isInitializing = true;
    private string _savedContent = string.Empty;
    private bool _isDirty;
    private bool _isHandlingClose;
    private bool _allowClose;

    public event EventHandler? PlacementChanged;

    public string? FilePath => _filePath;

    public NoteWindow(
        Note note,
        string? filePath,
        INoteFileService noteFileService,
        NoteWindowState? savedState = null)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(noteFileService);

        InitializeComponent();

        _note = note;
        _filePath = filePath;
        _noteFileService = noteFileService;
        _savedContent = note.Content;

        EditorTextBox.Text = _savedContent;
        _isInitializing = false;

        if (savedState is not null && IsValidVisiblePlacement(savedState))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = savedState.Left;
            Top = savedState.Top;
            Width = savedState.Width;
            Height = savedState.Height;
            Topmost = savedState.IsTopmost;
            AlwaysOnTopCheckBox.IsChecked = savedState.IsTopmost;
        }

        LocationChanged += OnPlacementChanged;
        SizeChanged += OnPlacementChanged;
        StateChanged += OnPlacementChanged;

        UpdateWindowTitle();
        StatusText.Text = filePath is null ? "Nota nueva" : "Archivo abierto";
    }

    public NoteWindowState? CaptureState()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            return null;
        }

        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return null;
        }

        return new NoteWindowState
        {
            FilePath = _filePath,
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            IsTopmost = Topmost
        };
    }

    private static bool IsValidVisiblePlacement(NoteWindowState state)
    {
        if (!double.IsFinite(state.Left) ||
            !double.IsFinite(state.Top) ||
            !double.IsFinite(state.Width) ||
            !double.IsFinite(state.Height) ||
            state.Width < 480 ||
            state.Height < 360)
        {
            return false;
        }

        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        var windowBounds = new Rect(state.Left, state.Top, state.Width, state.Height);
        return virtualScreen.IntersectsWith(windowBounds);
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        PlacementChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _note.Content = EditorTextBox.Text;
        _isDirty = !string.Equals(EditorTextBox.Text, _savedContent, StringComparison.Ordinal);
        StatusText.Text = _isDirty ? "Cambios sin guardar" : "Sin cambios";
        UpdateWindowTitle();
    }

    private void AlwaysOnTopCheckBox_Click(object sender, RoutedEventArgs e)
    {
        Topmost = AlwaysOnTopCheckBox.IsChecked == true;
        OnPlacementChanged(this, EventArgs.Empty);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveNoteAsync();
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S &&
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            e.Handled = true;
            await SaveNoteAsync();
        }
    }

    private async Task<bool> SaveNoteAsync()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Guardar nota",
                Filter = "Archivo de texto (*.txt)|*.txt|Archivo Markdown (*.md)|*.md",
                DefaultExt = ".txt",
                AddExtension = true,
                FileName = string.IsNullOrWhiteSpace(_note.Title) ? "Nueva nota" : _note.Title
            };

            if (dialog.ShowDialog(this) != true)
            {
                return false;
            }

            _filePath = dialog.FileName;
        }

        try
        {
            _note.Content = EditorTextBox.Text;
            await _noteFileService.SaveAsync(_note, _filePath);
            _note.Title = System.IO.Path.GetFileNameWithoutExtension(_filePath);
            _note.UpdatedAt = DateTimeOffset.UtcNow;
            _savedContent = EditorTextBox.Text;
            _isDirty = false;
            StatusText.Text = $"Guardado · {DateTime.Now:HH:mm}";
            UpdateWindowTitle();
            OnPlacementChanged(this, EventArgs.Empty);
            return true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"No se pudo guardar la nota.\n\n{exception.Message}",
                "Error al guardar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    public async Task<bool> RequestCloseAsync()
    {
        if (_allowClose)
        {
            return true;
        }

        if (_isHandlingClose)
        {
            return false;
        }

        _isHandlingClose = true;

        try
        {
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    this,
                    "Esta nota tiene cambios sin guardar. ¿Quieres guardarlos antes de cerrar?",
                    "Cambios sin guardar",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }

                if (result == MessageBoxResult.Yes && !await SaveNoteAsync())
                {
                    return false;
                }
            }

            _allowClose = true;
            Close();
            return true;
        }
        finally
        {
            _isHandlingClose = false;
        }
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        await RequestCloseAsync();
    }

    private void UpdateWindowTitle()
    {
        var title = string.IsNullOrWhiteSpace(_filePath)
            ? "Nueva nota"
            : System.IO.Path.GetFileName(_filePath);

        NoteTitleText.Text = title;
        Title = $"{(_isDirty ? "● " : string.Empty)}{title} - DeskNotes";
    }
}
