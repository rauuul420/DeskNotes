using System.Windows;
using DeskNotes.Core.Interfaces;
using DeskNotes.Core.Models;
using DeskNotes.Infrastructure.FileSystem;
using Microsoft.Win32;

namespace DeskNotes.App;

/// <summary>
/// Main application window. It acts as a launcher for independent note windows.
/// </summary>
public partial class MainWindow : Window
{
    private readonly INoteFileService _noteFileService = new NoteFileService();

    public MainWindow()
    {
        InitializeComponent();
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

        try
        {
            var note = await _noteFileService.OpenAsync(dialog.FileName);
            OpenNoteWindow(note, dialog.FileName);
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

    private void OpenNoteWindow(Note note, string? filePath)
    {
        var window = new NoteWindow(note, filePath, _noteFileService)
        {
            Owner = this
        };

        window.Show();
        window.Activate();
    }
}
