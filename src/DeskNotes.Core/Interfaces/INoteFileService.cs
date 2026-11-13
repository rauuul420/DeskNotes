using DeskNotes.Core.Models;

namespace DeskNotes.Core.Interfaces;

/// <summary>
/// Defines operations for opening and saving note files.
/// </summary>
public interface INoteFileService
{
    /// <summary>
    /// Opens a TXT or Markdown file and returns it as a note.
    /// </summary>
    Task<Note> OpenAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a note to the specified file path.
    /// </summary>
    Task SaveAsync(
        Note note,
        string filePath,
        CancellationToken cancellationToken = default);
}
