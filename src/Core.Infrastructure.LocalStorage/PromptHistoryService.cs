using Core.Application.McpServers;
using Core.Application.Storage;

namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// Service for managing prompt invocation history and drafts with localStorage persistence.
/// </summary>
public class PromptHistoryService : IPromptHistoryService
{
    private const string StorageKey = "jasmin-webui:prompt-history";
    private const int DefaultMaxHistoryItems = 20;

    private readonly ILocalStorageService _localStorage;
    private PromptInvocationHistory _history = new();
    private bool _isLoaded;
    private int _maxHistoryItems = DefaultMaxHistoryItems;

    public bool IsLoaded => _isLoaded;

    public int MaxHistoryItems
    {
        get => _maxHistoryItems;
        set => _maxHistoryItems = Math.Max(1, value);
    }

    public PromptHistoryService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (_isLoaded) return;

        var saved = await _localStorage.GetAsync<PromptInvocationHistory>(StorageKey);
        if (saved != null)
        {
            _history = saved;
        }
        _isLoaded = true;
    }

    private static string GetKey(string serverName, string promptName) => $"{serverName}:{promptName}";

    // History methods

    /// <inheritdoc />
    public IReadOnlyList<PromptHistoryEntry> GetHistory(string serverName, string promptName)
    {
        var key = GetKey(serverName, promptName);
        if (_history.History.TryGetValue(key, out var entries))
        {
            return entries;
        }
        return Array.Empty<PromptHistoryEntry>();
    }

    /// <inheritdoc />
    public async Task AddEntryAsync(string serverName, string promptName, Dictionary<string, string?> argumentValues, PromptInvocationResult? output)
    {
        var key = GetKey(serverName, promptName);

        if (!_history.History.TryGetValue(key, out var entries))
        {
            entries = new List<PromptHistoryEntry>();
            _history = _history with
            {
                History = new Dictionary<string, List<PromptHistoryEntry>>(_history.History)
                {
                    [key] = entries
                }
            };
        }

        // Create a copy of argument values and store output
        var entry = new PromptHistoryEntry
        {
            ArgumentValues = new Dictionary<string, string?>(argumentValues),
            Output = output,
            InvokedAt = DateTime.UtcNow
        };

        entries.Add(entry);

        // Trim to max items
        while (entries.Count > _maxHistoryItems)
        {
            entries.RemoveAt(0);
        }

        await SaveAsync();
    }

    /// <inheritdoc />
    public async Task ClearHistoryAsync(string serverName, string promptName)
    {
        var key = GetKey(serverName, promptName);
        if (_history.History.ContainsKey(key))
        {
            var newHistory = new Dictionary<string, List<PromptHistoryEntry>>(_history.History);
            newHistory.Remove(key);
            _history = _history with { History = newHistory };
            await SaveAsync();
        }
    }

    // Draft methods

    /// <inheritdoc />
    public Dictionary<string, string?>? GetDraft(string serverName, string promptName)
    {
        var key = GetKey(serverName, promptName);
        if (_history.Drafts.TryGetValue(key, out var draft))
        {
            // Return a copy to avoid external modification
            return new Dictionary<string, string?>(draft);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task SaveDraftAsync(string serverName, string promptName, Dictionary<string, string?> argumentValues)
    {
        var key = GetKey(serverName, promptName);

        var newDrafts = new Dictionary<string, Dictionary<string, string?>>(_history.Drafts)
        {
            [key] = new Dictionary<string, string?>(argumentValues)
        };
        _history = _history with { Drafts = newDrafts };

        await SaveAsync();
    }

    /// <inheritdoc />
    public async Task ClearDraftAsync(string serverName, string promptName)
    {
        var key = GetKey(serverName, promptName);
        if (_history.Drafts.ContainsKey(key))
        {
            var newDrafts = new Dictionary<string, Dictionary<string, string?>>(_history.Drafts);
            newDrafts.Remove(key);
            _history = _history with { Drafts = newDrafts };
            await SaveAsync();
        }
    }

    private Task SaveAsync()
    {
        return _localStorage.SetAsync(StorageKey, _history);
    }
}
