using DeskNotes.Core.Interfaces;
using DeskNotes.Core.Models;

namespace DeskNotes.Core.Services;

/// <summary>
/// Coordinates saving a note to its current file or to a new destination.
/// </summary>
public sealed class NoteSaveService
{
    private readonly INoteFileService _noteFileService;

    public NoteSaveService(INoteFileService noteFileService)
    {
        ArgumentNullException.ThrowIfNull(noteFileService);
        _noteFileService = noteFileService;
    }

    /// <summary>
    /// Saves a note. When the destination differs from its current path, a new
    /// file identity is used so the source file's fingerprint is not applied
    /// to the destination.
    /// </summary>
    public async Task SaveAsync(
        Note note,
        string? currentFilePath,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFilePath);

        var fullTargetPath = Path.GetFullPath(targetFilePath);
        var isSaveAs = !string.IsNullOrWhiteSpace(currentFilePath) &&
            !string.Equals(
                Path.GetFullPath(currentFilePath),
                fullTargetPath,
                StringComparison.OrdinalIgnoreCase);

        if (!isSaveAs)
        {
            await _noteFileService.SaveAsync(note, fullTargetPath, cancellationToken);
            return;
        }

        var copy = new Note
        {
            Title = Path.GetFileNameWithoutExtension(fullTargetPath),
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };

        await _noteFileService.SaveAsync(copy, fullTargetPath, cancellationToken);

        // Transfer the new destination's fingerprint only after the save succeeds.
        note.SourceLastWriteTimeUtc = copy.SourceLastWriteTimeUtc;
        note.SourceLength = copy.SourceLength;
        note.SourceSha256 = copy.SourceSha256;
    }
}
