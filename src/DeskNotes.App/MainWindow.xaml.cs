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
/// Main application window. It manages the saved-note library and restores the previous session.
/// </summary>
public partial class MainWindow : Window
{
    private readonly INoteFileService _noteFileService = new NoteFileService();
    private readonly NoteSaveService _noteSaveService;
    private readonly IAppSettingsService _settingsService = new JsonAppSettingsService();
    private readonly List<NoteWindow> _noteWindows = [];
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
        AddSavedNote(filePath, Path.GetFileNameWithoutExtension(filePath));
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

    private async Task SaveCurrentSettingsAsync()
    {
        var settings = new AppSettings
        {
            OpenNotes = _noteWindows
                .Select(window => window.CaptureState())
                .Where(state => state is not null)
                .Cast<NoteWindowState>()
                .ToList(),
            SavedNotes = _savedNotes.ToList()
        };

        try
        {
            await _settingsService.SaveAsync(settings);
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

        var settings = new AppSettings
        {
            OpenNotes = windowsToClose
                .Select(window => window.CaptureState())
                .Where(state => state is not null)
                .Cast<NoteWindowState>()
                .ToList(),
            SavedNotes = _savedNotes.ToList()
        };

        try
        {
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

        _allowClose = true;
        Close();
    }
}
