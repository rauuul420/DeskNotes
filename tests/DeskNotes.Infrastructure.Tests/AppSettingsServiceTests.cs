using DeskNotes.Core.Models;
using DeskNotes.Infrastructure.Persistence;
using Xunit;

namespace DeskNotes.Infrastructure.Tests;

public sealed class AppSettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_ReturnsDefaultSettingsWhenFileDoesNotExist()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var service = new JsonAppSettingsService(Path.Combine(directory, "settings.json"));

            var settings = await service.LoadAsync();

            Assert.NotNull(settings);
            Assert.Empty(settings.OpenNotes);
            Assert.Empty(settings.SavedNotes);
            Assert.Empty(settings.NoteWidgets);
            Assert.False(settings.DesktopLayerEnabled);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_PreserveDesktopLayerPreference()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var service = new JsonAppSettingsService(Path.Combine(directory, "settings.json"));
            var settings = new AppSettings { DesktopLayerEnabled = true };

            await service.SaveAsync(settings);
            var loaded = await service.LoadAsync();

            Assert.True(loaded.DesktopLayerEnabled);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_PreserveNoteWindowPlacements()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "settings.json");
            var service = new JsonAppSettingsService(path);
            var settings = new AppSettings
            {
                OpenNotes =
                [
                    new NoteWindowState
                    {
                        FilePath = @"C:\Notes\ideas.md",
                        Left = 120,
                        Top = 80,
                        Width = 640,
                        Height = 480,
                        IsTopmost = true
                    }
                ]
            };

            await service.SaveAsync(settings);
            var loaded = await service.LoadAsync();

            var window = Assert.Single(loaded.OpenNotes);
            Assert.Equal(@"C:\Notes\ideas.md", window.FilePath);
            Assert.Equal(120, window.Left);
            Assert.Equal(80, window.Top);
            Assert.Equal(640, window.Width);
            Assert.Equal(480, window.Height);
            Assert.True(window.IsTopmost);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_CreatesSettingsDirectoryAndDoesNotLeaveTemporaryFiles()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var settingsDirectory = Path.Combine(directory, "nested", "DeskNotes");
            var path = Path.Combine(settingsDirectory, "settings.json");
            var service = new JsonAppSettingsService(path);

            await service.SaveAsync(new AppSettings());

            Assert.True(File.Exists(path));
            Assert.Empty(Directory.GetFiles(settingsDirectory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_NormalizesNullSettingsCollections()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "settings.json");
            await File.WriteAllTextAsync(path, """{"openNotes": null, "savedNotes": null, "noteWidgets": null}""");
            var service = new JsonAppSettingsService(path);

            var settings = await service.LoadAsync();

            Assert.NotNull(settings.OpenNotes);
            Assert.Empty(settings.OpenNotes);
            Assert.NotNull(settings.SavedNotes);
            Assert.Empty(settings.SavedNotes);
            Assert.NotNull(settings.NoteWidgets);
            Assert.Empty(settings.NoteWidgets);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_PreserveDesktopWidgetPlacement()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var service = new JsonAppSettingsService(Path.Combine(directory, "settings.json"));
            var settings = new AppSettings
            {
                NoteWidgets =
                [
                    new NoteWidgetState
                    {
                        FilePath = @"C:\\Notes\\ideas.md",
                        Left = 145,
                        Top = 95,
                        Width = 360,
                        Height = 280,
                        IsTopmost = true
                    }
                ]
            };

            await service.SaveAsync(settings);
            var loaded = await service.LoadAsync();

            var widget = Assert.Single(loaded.NoteWidgets);
            Assert.Equal(settings.NoteWidgets[0].FilePath, widget.FilePath);
            Assert.Equal(145, widget.Left);
            Assert.Equal(95, widget.Top);
            Assert.Equal(360, widget.Width);
            Assert.Equal(280, widget.Height);
            Assert.True(widget.IsTopmost);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_PreserveSavedNotesIndependentlyFromOpenWindows()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var service = new JsonAppSettingsService(Path.Combine(directory, "settings.json"));
            var settings = new AppSettings
            {
                OpenNotes = [],
                SavedNotes =
                [
                    new SavedNote
                    {
                        FilePath = "C:\\Users\\User\\Documents\\ideas.md",
                        Title = "Ideas",
                        AddedAt = DateTimeOffset.Parse("2026-11-13T10:00:00Z")
                    }
                ]
            };

            await service.SaveAsync(settings);
            var loaded = await service.LoadAsync();

            Assert.Empty(loaded.OpenNotes);
            var savedNote = Assert.Single(loaded.SavedNotes);
            Assert.Equal(settings.SavedNotes[0].FilePath, savedNote.FilePath);
            Assert.Equal("Ideas", savedNote.Title);
            Assert.Equal(settings.SavedNotes[0].AddedAt, savedNote.AddedAt);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_SerializesConcurrentWritesWithoutCorruptingJson()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "settings.json");
            var service = new JsonAppSettingsService(path);

            var saves = Enumerable.Range(0, 8).Select(index =>
                service.SaveAsync(new AppSettings
                {
                    OpenNotes =
                    [
                        new NoteWindowState
                        {
                            FilePath = $@"C:\Notes\note-{index}.md",
                            Left = index * 10,
                            Top = index * 5
                        }
                    ]
                }));

            await Task.WhenAll(saves);

            var loaded = await service.LoadAsync();

            Assert.Single(loaded.OpenNotes);
            Assert.EndsWith(".md", loaded.OpenNotes[0].FilePath);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_ThrowsWhenSettingsFileContainsInvalidJson()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "settings.json");
            await File.WriteAllTextAsync(path, "{ invalid json");
            var service = new JsonAppSettingsService(path);

            await Assert.ThrowsAsync<System.Text.Json.JsonException>(
                () => service.LoadAsync());
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
            $"DeskNotes.Settings.Tests.{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);
        return path;
    }
}
