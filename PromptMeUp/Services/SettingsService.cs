// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}

public sealed class SettingsService(IDatabaseService database) : ISettingsService
{
    /// <summary>Loads the current application settings through the database boundary.</summary>
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => database.LoadSettingsAsync(cancellationToken);

    /// <summary>Persists a complete validated application settings replacement.</summary>
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) => database.SaveSettingsAsync(settings, cancellationToken);
}
