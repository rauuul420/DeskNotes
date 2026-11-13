using DeskNotes.Core.Models;

namespace DeskNotes.Core.Interfaces;

/// <summary>
/// Loads and saves application settings.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Loads application settings, returning defaults when no settings file exists.
    /// </summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves application settings.
    /// </summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
