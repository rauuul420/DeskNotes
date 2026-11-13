namespace DeskNotes.Core.Models;

/// <summary>
/// User-specific settings persisted between DeskNotes sessions.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets whether existing file-backed notes are saved automatically.
    /// </summary>
    public bool AutoSaveEnabled { get; set; }

    /// <summary>
    /// Gets or sets the idle interval, in seconds, before an automatic save.
    /// </summary>
    public int AutoSaveIntervalSeconds { get; set; } = 10;

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
