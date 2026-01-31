namespace Core.Application.Storage;

/// <summary>
/// Service for persisting data to browser local storage.
/// </summary>
public interface ILocalStorageService
{
    /// <summary>
    /// Gets a value from local storage.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Sets a value in local storage.
    /// </summary>
    Task SetAsync<T>(string key, T value);

    /// <summary>
    /// Removes a value from local storage.
    /// </summary>
    Task RemoveAsync(string key);
}
