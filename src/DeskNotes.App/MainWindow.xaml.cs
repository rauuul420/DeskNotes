using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DeskNotes.Core.Interfaces;
using DeskNotes.Core.Models;
using DeskNotes.Core.Services;
using DeskNotes.Infrastructure.FileSystem;
using DeskNotes.Infrastructure.Persistence;
using Microsoft.Win32;

namespace DeskNotes.App;

/// <summary>
/// Main application window. It manages the saved-note library, editor windows and desktop widgets.
/// </summary>
public partial class MainWindow : Window
{
    private readonly INoteFileService _noteFileService = new NoteFileService();
    private readonly NoteSaveService _noteSaveService;
    private readonly IAppSettingsService _settingsService = new JsonAppSettingsService();
    private readonly List<NoteWindow> _noteWindows = [];
    private readonly List<NoteWidgetWindow> _noteWidgets = [];
    private readonly ObservableCollection<SavedNote> _savedNotes = [];
    private readonly DispatcherTimer _settingsSaveTimer;
    private bool _isRestoring;
    private bool _isClosing;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        _noteSaveService = new NoteSaveService(_noteFileService);
        SavedNotesList.ItemsSource = _savedNotes;
        _savedNotes.CollectionChanged += (_, _) =>
            EmptyLibraryText.Visibility = _savedNotes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyLibraryText.Visibility = Visibility.Visible;

        _settingsSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        _settingsSaveTimer.Tick += SettingsSaveTimer_Tick;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _isRestoring = true;

        try
        {
            var settings = await _settingsService.LoadAsync();

            foreach (var savedNote in settings.SavedNotes)
            {
                if (string.IsNullOrWhiteSpace(savedNote.FilePath))
                {
                    continue;
                }

                try
                {
                    AddSavedNote(savedNote.FilePath, savedNote.Title, savedNote.AddedAt);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        this,
                        $"No se pudo cargar la entrada '{savedNote.FilePath}' de Mis notas.\n\n{exception.Message}",
                        "Entrada de biblioteca no válida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }

            var restoredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var savedWindow in settings.OpenNotes)
            {
                if (string.IsNullOrWhiteSpace(savedWindow.FilePath))
                {
                    continue;
                }

                try
                {
                    var fullPath = Path.GetFullPath(savedWindow.FilePath);

                    if (!restoredPaths.Add(fullPath) || !File.Exists(fullPath))
                    {
                        continue;
                    }

                    var note = await _noteFileService.OpenAsync(fullPath);
                    AddSavedNote(fullPath, note.Title);
                    OpenNoteWindow(note, fullPath, savedWindow);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        this,
                        $"No se pudo restaurar la nota '{savedWindow.FilePath}'.\n\n{exception.Message}",
                        "No se pudo restaurar una nota",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }

            var restoredWidgetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var widgetState in settings.NoteWidgets)
            {
                if (string.IsNullOrWhiteSpace(widgetState.FilePath))
                {
                    continue;
                }

                try
                {
                    var fullPath = Path.GetFullPath(widgetState.FilePath);

                    if (!restoredWidgetPaths.Add(fullPath) || !File.Exists(fullPath))
                    {
                        continue;
                    }

                    var openEditor = _noteWindows.FirstOrDefault(window =>
                        string.Equals(window.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

                    var note = openEditor is null
                        ? await _noteFileService.OpenAsync(fullPath)
                        : null;

                    OpenNoteWidget(
                        fullPath,
                        openEditor is null ? note!.Title : Path.GetFileNameWithoutExtension(fullPath),
                        openEditor?.CurrentContent ?? note!.Content,
                        widgetState);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        this,
                        $"No se pudo restaurar el widget de '{widgetState.FilePath}'.\n\n{exception.Message}",
                        "No se pudo restaurar un widget",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"No se pudo cargar la configuración de DeskNotes. Se iniciará una sesión nueva.\n\n{exception.Message}",
                "Error de configuración",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _isRestoring = false;
        }

        await SaveCurrentSettingsAsync();
    }

    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        OpenNoteWindow(new Note(), filePath: null);
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        NotesView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Visible;
    }

    private void BackToNotes_Click(object sender, RoutedEventArgs e)
    {
        SettingsView.Visibility = Visibility.Collapsed;
        NotesView.Visibility = Visibility.Visible;
    }

    private async void OpenNote_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Abrir una nota",
            Filter = "Archivos de texto (*.txt;*.md)|*.txt;*.md|Texto (*.txt)|*.txt|Markdown (*.md)|*.md",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        await OpenSavedNoteAsync(dialog.FileName);
    }

    private async void SavedNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string filePath })
        {
            return;
        }

        await OpenSavedNoteAsync(filePath);
    }

    private async Task OpenSavedNoteAsync(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var existingWindow = _noteWindows.FirstOrDefault(window =>
                string.Equals(window.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

            if (existingWindow is not null)
            {
                existingWindow.Activate();
                return;
            }

            if (!File.Exists(fullPath))
            {
                MessageBox.Show(
                    this,
                    $"No se encuentra el archivo de esta nota:\n\n{fullPath}\n\nSi lo has movido, usa «Abrir archivo» para abrir su nueva ubicación. También puedes quitar esta entrada de Mis notas; el archivo original no se eliminará.",
                    "Archivo no encontrado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var note = await _noteFileService.OpenAsync(fullPath);
            AddSavedNote(fullPath, note.Title);
            OpenNoteWindow(note, fullPath);
            await SaveCurrentSettingsAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"No se pudo abrir el archivo.\n\n{exception.Message}",
                "Error al abrir la nota",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void CreateWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string filePath })
        {
            return;
        }

        await CreateNoteWidgetAsync(filePath);
    }

    private async Task CreateNoteWidgetAsync(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var existingWidget = _noteWidgets.FirstOrDefault(widget =>
                string.Equals(widget.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

            if (existingWidget is not null)
            {
                existingWidget.Activate();
                return;
            }

            if (!File.Exists(fullPath))
            {
                MessageBox.Show(
                    this,
                    $"No se encuentra el archivo de esta nota:\n\n{fullPath}",
                    "Archivo no encontrado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var openEditor = _noteWindows.FirstOrDefault(window =>
                string.Equals(window.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

            var note = openEditor is null
                ? await _noteFileService.OpenAsync(fullPath)
                : null;

            OpenNoteWidget(
                fullPath,
                openEditor is null ? note!.Title : Path.GetFileNameWithoutExtension(fullPath),
                openEditor?.CurrentContent ?? note!.Content);

            await SaveCurrentSettingsAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"No se pudo crear el widget.\n\n{exception.Message}",
                "Error al crear widget",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenNoteWidget(
        string filePath,
        string title,
        string content,
        NoteWidgetState? savedState = null)
    {
        var fullPath = Path.GetFullPath(filePath);
        var existingWidget = _noteWidgets.FirstOrDefault(widget =>
            string.Equals(widget.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

        if (existingWidget is not null)
        {
            existingWidget.UpdateContent(title, content);
            existingWidget.Activate();
            return;
        }

        var widget = new NoteWidgetWindow(fullPath, title, content, savedState);
        widget.PlacementChanged += NoteWidget_PlacementChanged;
        widget.EditRequested += NoteWidget_EditRequested;
        widget.Closed += NoteWidget_Closed;
        _noteWidgets.Add(widget);

        widget.Show();
    }

    private void NoteWidget_EditRequested(NoteWidgetWindow widget)
    {
        _ = OpenSavedNoteAsync(widget.FilePath);
    }

    private void NoteWidget_PlacementChanged(object? sender, EventArgs e)
    {
        ScheduleSettingsSave();
    }

    private void NoteWidget_Closed(object? sender, EventArgs e)
    {
        if (sender is not NoteWidgetWindow widget)
        {
            return;
        }

        widget.PlacementChanged -= NoteWidget_PlacementChanged;
        widget.EditRequested -= NoteWidget_EditRequested;
        widget.Closed -= NoteWidget_Closed;
        _noteWidgets.Remove(widget);
        ScheduleSettingsSave();
    }

    private async void RemoveSavedNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string filePath })
        {
            return;
        }

        var entry = _savedNotes.FirstOrDefault(note =>
            string.Equals(note.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            return;
        }

        _savedNotes.Remove(entry);
        await SaveCurrentSettingsAsync();
    }

    private void OpenNoteWindow(
        Note note,
        string? filePath,
        NoteWindowState? savedState = null)
    {
        var window = new NoteWindow(note, filePath, _noteSaveService, savedState)
        {
            Owner = this
        };

        window.PlacementChanged += NoteWindow_PlacementChanged;
        window.SavedToFile += NoteWindow_SavedToFile;
        window.Closed += NoteWindow_Closed;
        _noteWindows.Add(window);

        window.Show();
        window.Activate();

        ScheduleSettingsSave();
    }

    private void NoteWindow_SavedToFile(NoteWindow window, string filePath)
    {
        var title = Path.GetFileNameWithoutExtension(filePath);
        AddSavedNote(filePath, title);

        var widget = _noteWidgets.FirstOrDefault(item =>
            string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        widget?.UpdateContent(title, window.CurrentContent);
        _ = SaveCurrentSettingsAsync();
    }

    private void AddSavedNote(
        string filePath,
        string? title = null,
        DateTimeOffset? addedAt = null)
    {
        var fullPath = Path.GetFullPath(filePath);
        var existing = _savedNotes.FirstOrDefault(note =>
            string.Equals(note.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Title = string.IsNullOrWhiteSpace(title)
                ? Path.GetFileNameWithoutExtension(fullPath)
                : title;
            return;
        }

        _savedNotes.Add(new SavedNote
        {
            FilePath = fullPath,
            Title = string.IsNullOrWhiteSpace(title)
                ? Path.GetFileNameWithoutExtension(fullPath)
                : title,
            AddedAt = addedAt ?? DateTimeOffset.UtcNow
        });
    }

    private void NoteWindow_PlacementChanged(object? sender, EventArgs e)
    {
        ScheduleSettingsSave();
    }

    private void NoteWindow_Closed(object? sender, EventArgs e)
    {
        if (sender is not NoteWindow window)
        {
            return;
        }

        window.PlacementChanged -= NoteWindow_PlacementChanged;
        window.SavedToFile -= NoteWindow_SavedToFile;
        window.Closed -= NoteWindow_Closed;
        _noteWindows.Remove(window);

        // The saved-note library is deliberately independent from open windows.
        ScheduleSettingsSave();
    }

    private void ScheduleSettingsSave()
    {
        if (_isRestoring || _isClosing)
        {
            return;
        }

        _settingsSaveTimer.Stop();
        _settingsSaveTimer.Start();
    }

    private async void SettingsSaveTimer_Tick(object? sender, EventArgs e)
    {
        _settingsSaveTimer.Stop();
        await SaveCurrentSettingsAsync();
    }

    private AppSettings CreateCurrentSettings()
    {
        return new AppSettings
        {
            OpenNotes = _noteWindows
                .Select(window => window.CaptureState())
                .Where(state => state is not null)
                .Cast<NoteWindowState>()
                .ToList(),
            SavedNotes = _savedNotes.ToList(),
            NoteWidgets = _noteWidgets
                .Select(widget => widget.CaptureState())
                .Where(state => state is not null)
                .Cast<NoteWidgetState>()
                .ToList()
        };
    }

    private async Task SaveCurrentSettingsAsync()
    {
        try
        {
            await _settingsService.SaveAsync(CreateCurrentSettings());
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"No se pudo guardar la configuración de DeskNotes.\n\n{exception.Message}",
                "Error al guardar la configuración",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;

        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        _settingsSaveTimer.Stop();

        var windowsToClose = _noteWindows.ToArray();

        foreach (var window in windowsToClose)
        {
            if (!await window.RequestCloseAsync())
            {
                _isClosing = false;
                await SaveCurrentSettingsAsync();
                return;
            }
        }

        var openNoteStates = windowsToClose
            .Select(window => window.CaptureState())
            .Where(state => state is not null)
            .Cast<NoteWindowState>()
            .ToList();

        try
        {
            var settings = CreateCurrentSettings();
            settings.OpenNotes = openNoteStates;

            await _settingsService.SaveAsync(settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"No se pudo guardar la sesión antes de salir.\n\n{exception.Message}",
                "Error al guardar la sesión",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            _isClosing = false;
            return;
        }

        // Cerrar las ventanas de notas cuyo cierre ya ha sido autorizado.
        foreach (var window in windowsToClose)
        {
            window.CloseAfterRequest();
        }

        // Cerrar todos los widgets.
        foreach (var widget in _noteWidgets.ToArray())
        {
            widget.Close();
        }

        _allowClose = true;
        Close();
    }
}
