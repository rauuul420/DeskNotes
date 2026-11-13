using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using DeskNotes.Core.Interfaces;
using DeskNotes.Core.Models;
using DeskNotes.Infrastructure.FileSystem;
using DeskNotes.Infrastructure.Persistence;
using Microsoft.Win32;

namespace DeskNotes.App;

/// <summary>
/// Main application window. It launches notes and restores the previous session.
/// </summary>
public partial class MainWindow : Window
{
    private readonly INoteFileService _noteFileService = new NoteFileService();
    private readonly IAppSettingsService _settingsService = new JsonAppSettingsService();
    private readonly List<NoteWindow> _noteWindows = [];
    private readonly DispatcherTimer _settingsSaveTimer;
    private bool _isRestoring;
    private bool _isClosing;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

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

            foreach (var savedWindow in settings.OpenNotes)
            {
                if (string.IsNullOrWhiteSpace(savedWindow.FilePath) ||
                    !File.Exists(savedWindow.FilePath))
                {
                    continue;
                }

                try
                {
                    var fullPath = Path.GetFullPath(savedWindow.FilePath);
                    var note = await _noteFileService.OpenAsync(fullPath);
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

        var fullPath = Path.GetFullPath(dialog.FileName);
        var existingWindow = _noteWindows.FirstOrDefault(window =>
            string.Equals(window.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

        if (existingWindow is not null)
        {
            existingWindow.Activate();
            return;
        }

        try
        {
            var note = await _noteFileService.OpenAsync(fullPath);
            OpenNoteWindow(note, fullPath);
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

    private void OpenNoteWindow(
        Note note,
        string? filePath,
        NoteWindowState? savedState = null)
    {
        var window = new NoteWindow(note, filePath, _noteFileService, savedState)
        {
            Owner = this
        };

        window.PlacementChanged += NoteWindow_PlacementChanged;
        window.Closed += NoteWindow_Closed;
        _noteWindows.Add(window);

        window.Show();
        window.Activate();

        ScheduleSettingsSave();
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
        window.Closed -= NoteWindow_Closed;
        _noteWindows.Remove(window);

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
                .ToList()
        };

        try
        {
            await _settingsService.SaveAsync(settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"No se pudo guardar la configuración de las ventanas.\n\n{exception.Message}",
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
                .ToList()
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
