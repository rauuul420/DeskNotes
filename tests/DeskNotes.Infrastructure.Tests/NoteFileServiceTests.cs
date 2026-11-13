using DeskNotes.Core.Models;
using DeskNotes.Infrastructure.FileSystem;
using Xunit;

namespace DeskNotes.Infrastructure.Tests;

public sealed class NoteFileServiceTests
{
    private readonly NoteFileService _service = new();

    [Theory]
    [InlineData(".txt")]
    [InlineData(".md")]
    public async Task OpenAsync_ReadsSupportedFile(string extension)
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, $"test{extension}");
            await File.WriteAllTextAsync(path, "Contenido de prueba");

            var note = await _service.OpenAsync(path);

            Assert.Equal("test", note.Title);
            Assert.Equal("Contenido de prueba", note.Content);
            Assert.NotEqual(Guid.Empty, note.Id);
            Assert.NotEqual(default, note.CreatedAt);
            Assert.NotEqual(default, note.UpdatedAt);
            Assert.NotNull(note.SourceLastWriteTimeUtc);
            Assert.NotNull(note.SourceLength);
            Assert.NotNull(note.SourceSha256);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WritesNoteContentAsUtf8WithoutBom()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "note.txt");
            var note = new Note { Content = "Hola, DeskNotes" };

            await _service.SaveAsync(note, path);

            var bytes = await File.ReadAllBytesAsync(path);

            Assert.Equal("Hola, DeskNotes", await File.ReadAllTextAsync(path));
            Assert.False(bytes.Length >= 3 &&
                         bytes[0] == 0xEF &&
                         bytes[1] == 0xBB &&
                         bytes[2] == 0xBF);
            Assert.NotNull(note.SourceLastWriteTimeUtc);
            Assert.Equal((long)bytes.Length, note.SourceLength!.Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_DoesNotOverwriteFileChangedOutsideDeskNotes()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "note.txt");
            await File.WriteAllTextAsync(path, "Contenido original");

            var note = await _service.OpenAsync(path);
            note.Content = "Cambios realizados en DeskNotes";

            await File.WriteAllTextAsync(path, "Contenido modificado desde otra aplicación");

            await Assert.ThrowsAsync<IOException>(
                () => _service.SaveAsync(note, path));

            Assert.Equal(
                "Contenido modificado desde otra aplicación",
                await File.ReadAllTextAsync(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }


    [Fact]
    public async Task SaveAsync_DetectsExternalChangeEvenWhenLengthAndTimestampAreUnchanged()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "note.txt");
            await File.WriteAllTextAsync(path, "Contenido original");

            var note = await _service.OpenAsync(path);
            var originalTimestamp = File.GetLastWriteTimeUtc(path);
            var originalLength = new FileInfo(path).Length;

            note.Content = "Cambios desde DeskNotes";
            await File.WriteAllTextAsync(path, "Texto alternativo!");

            Assert.Equal(originalLength, new FileInfo(path).Length);
            File.SetLastWriteTimeUtc(path, originalTimestamp);

            await Assert.ThrowsAsync<IOException>(
                () => _service.SaveAsync(note, path));

            Assert.Equal("Texto alternativo!", await File.ReadAllTextAsync(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task OpenAsync_RejectsUnsupportedFileExtension()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "note.json");
            await File.WriteAllTextAsync(path, "{}");

            await Assert.ThrowsAsync<NotSupportedException>(
                () => _service.OpenAsync(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsUnsupportedFileExtension()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "note.json");
            var note = new Note { Content = "{}" };

            await Assert.ThrowsAsync<NotSupportedException>(
                () => _service.SaveAsync(note, path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }


    [Fact]
    public async Task OpenAsync_DetectsUtf8BomAndReadsUnicode()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "unicode.txt");
            var content = "Español: acción, corazón, pingüino";
            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(content))
                .ToArray();

            await File.WriteAllBytesAsync(path, bytes);

            var note = await _service.OpenAsync(path);

            Assert.Equal(content, note.Content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_AllowsSubsequentSaveAfterOpeningNewFile()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "note.md");
            var newNote = new Note { Content = "Primera versión" };

            await _service.SaveAsync(newNote, path);

            var openedNote = await _service.OpenAsync(path);
            openedNote.Content = "Segunda versión";

            await _service.SaveAsync(openedNote, path);

            Assert.Equal("Segunda versión", await File.ReadAllTextAsync(path));
            Assert.NotNull(openedNote.SourceSha256);
            Assert.Equal((long)new System.IO.FileInfo(path).Length, openedNote.SourceLength!.Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_DoesNotRecreateSourceFileDeletedOutsideDeskNotes()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "note.txt");
            await File.WriteAllTextAsync(path, "Contenido original");

            var note = await _service.OpenAsync(path);
            note.Content = "Cambios en DeskNotes";
            File.Delete(path);

            await Assert.ThrowsAsync<IOException>(
                () => _service.SaveAsync(note, path));

            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"DeskNotes.Tests.{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);
        return path;
    }
}
