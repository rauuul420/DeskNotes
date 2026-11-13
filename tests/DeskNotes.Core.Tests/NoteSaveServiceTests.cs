using System.Text;
using DeskNotes.Core.Interfaces;
using DeskNotes.Core.Models;
using DeskNotes.Core.Services;
using Xunit;

namespace DeskNotes.Core.Tests;

public sealed class NoteSaveServiceTests
{
    [Fact]
    public async Task SaveAs_UsesIndependentFileIdentityAndTransfersFingerprintAfterSuccess()
    {
        var fileService = new RecordingNoteFileService();
        var service = new NoteSaveService(fileService);
        var originalTimestamp = DateTimeOffset.UtcNow.AddDays(-1);
        var note = new Note
        {
            Title = "Original",
            Content = "Contenido actualizado",
            CreatedAt = originalTimestamp,
            SourceLastWriteTimeUtc = originalTimestamp,
            SourceLength = 12,
            SourceSha256 = "original-hash"
        };
        var originalId = note.Id;
        var sourcePath = Path.Combine(Path.GetTempPath(), "original.txt");
        var destinationPath = Path.Combine(Path.GetTempPath(), "copy.md");

        await service.SaveAsync(note, sourcePath, destinationPath);

        Assert.NotNull(fileService.SavedNote);
        Assert.NotSame(note, fileService.SavedNote);
        Assert.NotEqual(originalId, fileService.SavedNote!.Id);
        Assert.Equal("copy", fileService.SavedNote.Title);
        Assert.Equal(note.Content, fileService.SavedNote.Content);
        Assert.Equal(originalTimestamp, fileService.SavedNote.CreatedAt);
        Assert.Equal(Path.GetFullPath(destinationPath), fileService.SavedPath);
        Assert.Equal("new-hash", note.SourceSha256);
        Assert.Equal((long)Encoding.UTF8.GetByteCount(note.Content), note.SourceLength!.Value);
        Assert.Equal(fileService.SavedNote.SourceLastWriteTimeUtc, note.SourceLastWriteTimeUtc);
    }

    [Fact]
    public async Task SaveAs_WhenWriteFails_DoesNotChangeOriginalFingerprint()
    {
        var fileService = new RecordingNoteFileService
        {
            ExceptionToThrow = new IOException("Simulated write failure")
        };
        var service = new NoteSaveService(fileService);
        var originalTimestamp = DateTimeOffset.UtcNow.AddDays(-1);
        var note = new Note
        {
            Content = "Cambios",
            SourceLastWriteTimeUtc = originalTimestamp,
            SourceLength = 7,
            SourceSha256 = "original-hash"
        };

        await Assert.ThrowsAsync<IOException>(() =>
            service.SaveAsync(
                note,
                Path.Combine(Path.GetTempPath(), "original.txt"),
                Path.Combine(Path.GetTempPath(), "copy.txt")));

        Assert.Equal(originalTimestamp, note.SourceLastWriteTimeUtc);
        Assert.Equal(7, note.SourceLength);
        Assert.Equal("original-hash", note.SourceSha256);
    }

    [Fact]
    public async Task SaveToCurrentPath_UsesOriginalNoteInstance()
    {
        var fileService = new RecordingNoteFileService();
        var service = new NoteSaveService(fileService);
        var note = new Note { Content = "Contenido" };
        var path = Path.Combine(Path.GetTempPath(), "note.txt");

        await service.SaveAsync(note, path, path);

        Assert.Same(note, fileService.SavedNote);
    }

    [Fact]
    public async Task SaveNewNote_UsesOriginalNoteInstance()
    {
        var fileService = new RecordingNoteFileService();
        var service = new NoteSaveService(fileService);
        var note = new Note { Content = "Nueva nota" };
        var path = Path.Combine(Path.GetTempPath(), "new-note.txt");

        await service.SaveAsync(note, null, path);

        Assert.Same(note, fileService.SavedNote);
    }

    private sealed class RecordingNoteFileService : INoteFileService
    {
        public Note? SavedNote { get; private set; }
        public string? SavedPath { get; private set; }
        public Exception? ExceptionToThrow { get; init; }

        public Task<Note> OpenAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            Note note,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            SavedNote = note;
            SavedPath = filePath;
            note.SourceLastWriteTimeUtc = DateTimeOffset.UtcNow;
            note.SourceLength = Encoding.UTF8.GetByteCount(note.Content);
            note.SourceSha256 = "new-hash";
            return Task.CompletedTask;
        }
    }
}
