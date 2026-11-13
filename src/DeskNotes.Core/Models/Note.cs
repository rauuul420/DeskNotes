namespace DeskNotes.Core.Models;

/// <summary>
/// Represents a note managed by DeskNotes.
/// </summary>
public sealed class Note
{
    /// <summary>
    /// Gets or sets the unique identifier of the note.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the note title.
    /// </summary>
    public string Title { get; set; } = "Nueva nota";

    /// <summary>
    /// Gets or sets the note content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the note was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the note was last updated (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
