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

    public NoteWindow(Note note, string? filePath, INoteFileService noteFileService)
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
        UpdateWindowTitle();
        StatusText.Text = filePath is null ? "Nota nueva" : "Archivo abierto";
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

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose || !_isDirty)
        {
            return;
        }

        e.Cancel = true;

        if (_isHandlingClose)
        {
            return;
        }

        _isHandlingClose = true;

        try
        {
            var result = MessageBox.Show(
                this,
                "Esta nota tiene cambios sin guardar. ¿Quieres guardarlos antes de cerrar?",
                "Cambios sin guardar",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            if (result == MessageBoxResult.Yes && !await SaveNoteAsync())
            {
                return;
            }

            _allowClose = true;
            Close();
        }
        finally
        {
            _isHandlingClose = false;
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
