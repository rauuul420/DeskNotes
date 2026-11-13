namespace DeskNotes.Core.Models;

/// <summary>
/// Stores the last known placement and presentation options of a desktop note widget.
/// </summary>
public sealed class NoteWidgetState
{
    /// <summary>
    /// Gets or sets the path of the note displayed by this widget.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the left coordinate of the widget.
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// Gets or sets the top coordinate of the widget.
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// Gets or sets the widget width.
    /// </summary>
    public double Width { get; set; } = 320;

    /// <summary>
    /// Gets or sets the widget height.
    /// </summary>
    public double Height { get; set; } = 240;

    /// <summary>
    /// Gets or sets whether the widget stays above other windows.
    /// </summary>
    public bool IsTopmost { get; set; }
}
