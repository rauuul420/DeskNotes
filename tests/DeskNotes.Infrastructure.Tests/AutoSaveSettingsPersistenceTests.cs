using DeskNotes.Core.Models;
using DeskNotes.Infrastructure.Persistence;
using Xunit;

namespace DeskNotes.Infrastructure.Tests;

public sealed class AutoSaveSettingsPersistenceTests
{
    [Fact]
    public async Task SaveAndLoad_PreservesAutoSavePreferences()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"DeskNotes.Tests.{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "settings.json");

        try
        {
            var service = new JsonAppSettingsService(filePath);
            await service.SaveAsync(new AppSettings
            {
                AutoSaveEnabled = true,
                AutoSaveIntervalSeconds = 30
            });

            var loaded = await service.LoadAsync();

            Assert.True(loaded.AutoSaveEnabled);
            Assert.Equal(30, loaded.AutoSaveIntervalSeconds);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Load_LegacySettings_UsesSafeAutoSaveDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"DeskNotes.Tests.{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "settings.json");

        try
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(filePath, """{"OpenNotes":[],"SavedNotes":[],"NoteWidgets":[]}""");

            var service = new JsonAppSettingsService(filePath);
            var loaded = await service.LoadAsync();

            Assert.False(loaded.AutoSaveEnabled);
            Assert.Equal(10, loaded.AutoSaveIntervalSeconds);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
