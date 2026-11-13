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
}
