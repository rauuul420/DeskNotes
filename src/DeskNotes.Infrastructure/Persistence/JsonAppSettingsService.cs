using System.Text.Json;
using DeskNotes.Core.Interfaces;
using DeskNotes.Core.Models;

namespace DeskNotes.Infrastructure.Persistence;

/// <summary>
/// Persists application settings as JSON in the user's local application data folder.
/// </summary>
public sealed class JsonAppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public JsonAppSettingsService(string? filePath = null)
    {
        _filePath = Path.GetFullPath(
            filePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DeskNotes",
                "settings.json"));
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new AppSettings();
        }

        await using var stream = new FileStream(
            _filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
            stream,
            SerializerOptions,
            cancellationToken);

        settings ??= new AppSettings();
        settings.OpenNotes ??= [];
        settings.SavedNotes ??= [];
        settings.NoteWidgets ??= [];
        return settings;
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _saveLock.WaitAsync(cancellationToken);

        var directory = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException("The settings path does not contain a directory.");
        var temporaryPath = Path.Combine(
            directory,
            $".settings.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(directory);

            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    SerializerOptions,
                    cancellationToken);

                await stream.FlushAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }
    }
}
