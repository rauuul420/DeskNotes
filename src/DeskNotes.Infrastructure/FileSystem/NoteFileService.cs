using System.Text;
using DeskNotes.Core.Interfaces;
using DeskNotes.Core.Models;

namespace DeskNotes.Infrastructure.FileSystem;

/// <summary>
/// Reads and writes TXT and Markdown note files.
/// </summary>
public sealed class NoteFileService : INoteFileService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public async Task<Note> OpenAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ValidateAndGetFullPath(filePath);

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);

        return new Note
        {
            Title = Path.GetFileNameWithoutExtension(fullPath),
            Content = content,
            CreatedAt = File.GetCreationTimeUtc(fullPath),
            UpdatedAt = File.GetLastWriteTimeUtc(fullPath)
        };
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        Note note,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        var fullPath = ValidateAndGetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The file path does not contain a directory.");

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                note.Content,
                Utf8WithoutBom,
                cancellationToken);

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string ValidateAndGetFullPath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = Path.GetFullPath(filePath);
        var extension = Path.GetExtension(fullPath);

        if (!extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "DeskNotes currently supports only .txt and .md files.");
        }

        return fullPath;
    }
}
