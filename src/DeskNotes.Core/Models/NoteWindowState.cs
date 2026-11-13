namespace DeskNotes.Core.Models;

/// <summary>
/// Stores the last known placement of a note window.
/// </summary>
public sealed class NoteWindowState
{
    /// <summary>
    /// Gets or sets the path of the note file associated with this window.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the left coordinate of the window.
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// Gets or sets the top coordinate of the window.
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// Gets or sets the window width.
    /// </summary>
    public double Width { get; set; } = 760;

    /// <summary>
    /// Gets or sets the window height.
    /// </summary>
    public double Height { get; set; } = 600;

    /// <summary>
    /// Gets or sets whether the window should remain above other windows.
    /// </summary>
    public bool IsTopmost { get; set; }
}
