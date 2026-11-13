using System.Security.Cryptography;
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
        var sourceInfo = new FileInfo(fullPath);

        if (!sourceInfo.Exists)
        {
            throw new FileNotFoundException("The note file could not be found.", fullPath);
        }

        var lastWriteTimeUtc = sourceInfo.LastWriteTimeUtc;
        var sourceLength = sourceInfo.Length;
        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        var content = DecodeText(bytes);
        var sourceHash = Convert.ToHexString(SHA256.HashData(bytes));

        sourceInfo.Refresh();

        if (!sourceInfo.Exists ||
            sourceInfo.LastWriteTimeUtc != lastWriteTimeUtc ||
            sourceInfo.Length != sourceLength)
        {
            throw new IOException("The file changed while DeskNotes was opening it. Please open it again.");
        }

        return new Note
        {
            Title = Path.GetFileNameWithoutExtension(fullPath),
            Content = content,
            CreatedAt = File.GetCreationTimeUtc(fullPath),
            UpdatedAt = lastWriteTimeUtc,
            SourceLastWriteTimeUtc = lastWriteTimeUtc,
            SourceLength = sourceLength,
            SourceSha256 = sourceHash
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

        var contentBytes = Utf8WithoutBom.GetBytes(note.Content);

        try
        {
            await File.WriteAllBytesAsync(
                temporaryPath,
                contentBytes,
                cancellationToken);

            if (note.SourceLastWriteTimeUtc is not null &&
                note.SourceLength is not null &&
                note.SourceSha256 is not null)
            {
                var currentSource = new FileInfo(fullPath);
                currentSource.Refresh();

                if (!currentSource.Exists ||
                    currentSource.LastWriteTimeUtc != note.SourceLastWriteTimeUtc.Value.UtcDateTime ||
                    currentSource.Length != note.SourceLength.Value)
                {
                    throw CreateExternalChangeException();
                }

                var currentBytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                var currentHash = Convert.ToHexString(SHA256.HashData(currentBytes));

                if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(currentHash),
                    Convert.FromHexString(note.SourceSha256)))
                {
                    throw CreateExternalChangeException();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);

            var savedFile = new FileInfo(fullPath);
            savedFile.Refresh();

            note.SourceLastWriteTimeUtc = savedFile.LastWriteTimeUtc;
            note.SourceLength = contentBytes.Length;
            note.SourceSha256 = Convert.ToHexString(SHA256.HashData(contentBytes));
            note.UpdatedAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string DecodeText(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    private static IOException CreateExternalChangeException() =>
        new(
            "This file has changed outside DeskNotes. Your edits have not been saved. " +
            "Reopen the file and review the latest version before trying again.");

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
