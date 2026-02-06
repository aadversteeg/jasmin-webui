using Core.Application.McpServers;
using Core.Application.Storage;

namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// Service for managing tool invocation history and drafts with localStorage persistence.
/// </summary>
public class ToolHistoryService : IToolHistoryService
{
    private const string StorageKey = "jasmin-webui:tool-history";
    private const int DefaultMaxHistoryItems = 20;

    private readonly ILocalStorageService _localStorage;
    private ToolInvocationHistory _history = new();
    private bool _isLoaded;
    private int _maxHistoryItems = DefaultMaxHistoryItems;

    public bool IsLoaded => _isLoaded;

    public int MaxHistoryItems
    {
        get => _maxHistoryItems;
        set => _maxHistoryItems = Math.Max(1, value);
    }

    public ToolHistoryService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (_isLoaded) return;

        var saved = await _localStorage.GetAsync<ToolInvocationHistory>(StorageKey);
        if (saved != null)
        {
            _history = saved;
        }
        _isLoaded = true;
    }

    private static string GetKey(string serverName, string toolName) => $"{serverName}:{toolName}";

    // History methods

    /// <inheritdoc />
    public IReadOnlyList<ToolHistoryEntry> GetHistory(string serverName, string toolName)
    {
        var key = GetKey(serverName, toolName);
        if (_history.History.TryGetValue(key, out var entries))
        {
            return entries;
        }
        return Array.Empty<ToolHistoryEntry>();
    }

    /// <inheritdoc />
    public async Task AddEntryAsync(string serverName, string toolName, Dictionary<string, object?> inputValues, ToolInvocationResult? output)
    {
        var key = GetKey(serverName, toolName);

        if (!_history.History.TryGetValue(key, out var entries))
        {
            entries = new List<ToolHistoryEntry>();
            _history = _history with
            {
                History = new Dictionary<string, List<ToolHistoryEntry>>(_history.History)
                {
                    [key] = entries
                }
            };
        }

        // Create a copy of input values and store output
        var entry = new ToolHistoryEntry
        {
            InputValues = new Dictionary<string, object?>(inputValues),
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
    public async Task ClearHistoryAsync(string serverName, string toolName)
    {
        var key = GetKey(serverName, toolName);
        if (_history.History.ContainsKey(key))
        {
            var newHistory = new Dictionary<string, List<ToolHistoryEntry>>(_history.History);
            newHistory.Remove(key);
            _history = _history with { History = newHistory };
            await SaveAsync();
        }
    }

    // Draft methods

    /// <inheritdoc />
    public Dictionary<string, object?>? GetDraft(string serverName, string toolName)
    {
        var key = GetKey(serverName, toolName);
        if (_history.Drafts.TryGetValue(key, out var draft))
        {
            // Return a copy to avoid external modification
            return new Dictionary<string, object?>(draft);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task SaveDraftAsync(string serverName, string toolName, Dictionary<string, object?> inputValues)
    {
        var key = GetKey(serverName, toolName);

        var newDrafts = new Dictionary<string, Dictionary<string, object?>>(_history.Drafts)
        {
            [key] = new Dictionary<string, object?>(inputValues)
        };
        _history = _history with { Drafts = newDrafts };

        await SaveAsync();
    }

    /// <inheritdoc />
    public async Task ClearDraftAsync(string serverName, string toolName)
    {
        var key = GetKey(serverName, toolName);
        if (_history.Drafts.ContainsKey(key))
        {
            var newDrafts = new Dictionary<string, Dictionary<string, object?>>(_history.Drafts);
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
