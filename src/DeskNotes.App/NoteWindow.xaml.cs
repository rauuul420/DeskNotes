using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DeskNotes.Core.Services;
using DeskNotes.Core.Models;
using Microsoft.Win32;

namespace DeskNotes.App;

/// <summary>
/// An independent editor window for one TXT or Markdown note.
/// </summary>
public partial class NoteWindow : Window
{
    private readonly Note _note;
    private readonly NoteSaveService _noteSaveService;
    private string? _filePath;
    private bool _isInitializing = true;
    private string _savedContent = string.Empty;
    private bool _isDirty;
    private bool _isHandlingClose;
    private bool _allowClose;
    private readonly DispatcherTimer _autoSaveTimer;
    private bool _autoSaveEnabled;

    public event EventHandler? PlacementChanged;

    public event Action<NoteWindow, string>? SavedToFile;

    public string? FilePath => _filePath;

    public string CurrentContent => EditorTextBox.Text;

    public NoteWindow(
        Note note,
        string? filePath,
        NoteSaveService noteSaveService,
        NoteWindowState? savedState = null,
        bool autoSaveEnabled = false,
        int autoSaveIntervalSeconds = 10)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(noteSaveService);

        InitializeComponent();

        _autoSaveTimer = new DispatcherTimer();
        _autoSaveTimer.Tick += AutoSaveTimer_Tick;

        _note = note;
        _filePath = filePath;
        _noteSaveService = noteSaveService;
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
        ConfigureAutoSave(autoSaveEnabled, autoSaveIntervalSeconds);
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
        var visibleBounds = Rect.Intersect(virtualScreen, windowBounds);

        return !visibleBounds.IsEmpty &&
               visibleBounds.Width >= 100 &&
               visibleBounds.Height >= 100;
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
        ScheduleAutoSave();
    }

    public void ConfigureAutoSave(bool enabled, int intervalSeconds)
    {
        if (intervalSeconds is not (5 or 10 or 30 or 60)) intervalSeconds = 10;
        _autoSaveEnabled = enabled;
        _autoSaveTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
        if (enabled) ScheduleAutoSave(); else _autoSaveTimer.Stop();
    }

    private void ScheduleAutoSave()
    {
        _autoSaveTimer.Stop();
        if (_autoSaveEnabled && _isDirty && !string.IsNullOrWhiteSpace(_filePath)) _autoSaveTimer.Start();
    }

    private async void AutoSaveTimer_Tick(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();
        if (!_autoSaveEnabled || !_isDirty || string.IsNullOrWhiteSpace(_filePath)) return;
        await SaveNoteAsync(showErrors: false);
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

    private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveNoteAsync(saveAs: true);
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.S || !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            return;
        }

        e.Handled = true;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            await SaveNoteAsync(saveAs: true);
        }
        else
        {
            await SaveNoteAsync();
        }
    }

    private async Task<bool> SaveNoteAsync(bool saveAs = false, bool showErrors = true)
    {
        var targetPath = _filePath;

        if (saveAs || string.IsNullOrWhiteSpace(targetPath))
        {
            var dialog = new SaveFileDialog
            {
                Title = saveAs ? "Guardar nota como" : "Guardar nota",
                Filter = "Archivo de texto (*.txt)|*.txt|Archivo Markdown (*.md)|*.md",
                DefaultExt = ".txt",
                AddExtension = true,
                FileName = string.IsNullOrWhiteSpace(_filePath)
                    ? (string.IsNullOrWhiteSpace(_note.Title) ? "Nueva nota" : _note.Title)
                    : System.IO.Path.GetFileName(_filePath)
            };

            if (dialog.ShowDialog(this) != true)
            {
                return false;
            }

            targetPath = System.IO.Path.GetFullPath(dialog.FileName);
        }

        var destinationPath = targetPath
            ?? throw new InvalidOperationException("A destination path is required to save a note.");

        try
        {
            _note.Content = EditorTextBox.Text;

            await _noteSaveService.SaveAsync(_note, _filePath, destinationPath);

            // Keep the current document path unchanged until the write succeeds.
            _filePath = destinationPath;
            _note.Title = System.IO.Path.GetFileNameWithoutExtension(destinationPath);
            _note.UpdatedAt = DateTimeOffset.UtcNow;
            _savedContent = EditorTextBox.Text;
            _isDirty = false;
            _autoSaveTimer.Stop();
            StatusText.Text = $"Guardado · {DateTime.Now:HH:mm}";
            UpdateWindowTitle();
            OnPlacementChanged(this, EventArgs.Empty);
            SavedToFile?.Invoke(this, destinationPath);
            return true;
        }
        catch (Exception exception)
        {
            if (showErrors)
            {
                MessageBox.Show(
                    this,
                    $"No se pudo guardar la nota.\\n\\n{exception.Message}",
                    "Error al guardar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                StatusText.Text = "Error en el guardado automático";
            }

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

            return true;
        }
        finally
        {
            _isHandlingClose = false;
        }
    }


    internal void CloseAfterRequest()
    {
        if (_allowClose)
        {
            return;
        }

        _allowClose = true;
        Close();
    }


    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        // Cancelamos el cierre inicial.
        e.Cancel = true;

        // Comprobamos si se puede cerrar y guardamos los cambios.
        bool canClose = await RequestCloseAsync();

        if (canClose)
        {
            // Programamos el cierre autorizado.
            _ = Dispatcher.BeginInvoke(new Action(CloseAfterRequest));        
        }
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
