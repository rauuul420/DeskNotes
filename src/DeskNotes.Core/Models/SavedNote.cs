namespace DeskNotes.Core.Models;

/// <summary>
/// A file-backed note registered in the user's DeskNotes library.
/// The file remains the source of truth for the note content.
/// </summary>
public sealed class SavedNote
{
    /// <summary>
    /// Gets or sets the full path to the TXT or Markdown file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display title shown in the library.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the file was added to the library (UTC).
    /// </summary>
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
