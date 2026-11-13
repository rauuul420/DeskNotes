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

    /// <summary>
    /// Gets or sets the last known modification time of the source file.
    /// Null means the note has not been loaded from an existing file.
    /// </summary>
    public DateTimeOffset? SourceLastWriteTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the last known source file contents.
    /// </summary>
    public string? SourceSha256 { get; set; }

    /// <summary>
    /// Gets or sets the last known size of the source file in bytes.
    /// </summary>
    public long? SourceLength { get; set; }
}
