namespace DeskNotes.Core.Models;

/// <summary>
/// User-specific settings persisted between DeskNotes sessions.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the note windows that should be restored on the next launch.
    /// </summary>
    public List<NoteWindowState> OpenNotes { get; set; } = [];

    /// <summary>
    /// Gets or sets the independent library of saved note files.
    /// Closing a note window does not remove its library entry.
    /// </summary>
    public List<SavedNote> SavedNotes { get; set; } = [];

    /// <summary>
    /// Gets or sets the desktop widgets that should be restored on the next launch.
    /// </summary>
    public List<NoteWidgetState> NoteWidgets { get; set; } = [];
}
