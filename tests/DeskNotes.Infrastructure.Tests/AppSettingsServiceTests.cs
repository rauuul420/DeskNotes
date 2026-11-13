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
    public async Task LoadAsync_NormalizesNullOpenNotesCollection()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var path = Path.Combine(directory, "settings.json");
            await File.WriteAllTextAsync(path, "{ \"openNotes\": null }");
            var service = new JsonAppSettingsService(path);

            var settings = await service.LoadAsync();

            Assert.NotNull(settings.OpenNotes);
            Assert.Empty(settings.OpenNotes);
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
