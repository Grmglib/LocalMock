using LocalMock.Models;
using Newtonsoft.Json.Linq;

namespace LocalMock.Services;

public class MockService : IMockService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<MockService> _logger;

    public MockService(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<MockService> logger)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public Task AddAsync(
        string? collection,
        string method,
        string path,
        int statusCode,
        int responseDelayMs,
        object? responseBody,
        string? responseContentType,
        bool enabled,
        bool bypassEnabled,
        string? bypassUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionReference(collection);
        var normalizedPath = MockStorePersistence.NormalizePath(path);
        var entry = new MockEntry
        {
            Collection = normalizedCollection,
            Method = method.ToUpperInvariant(),
            Path = normalizedPath,
            StatusCode = statusCode,
            ResponseDelayMs = MockStorePersistence.NormalizeResponseDelayMs(responseDelayMs),
            ResponseContentType = MockStorePersistence.NormalizeResponseContentType(responseContentType),
            ResponseBody = ToResponseBodyToken(responseBody),
            Enabled = string.IsNullOrWhiteSpace(normalizedCollection) || enabled,
            BypassEnabled = bypassEnabled,
            BypassUrl = MockStorePersistence.NormalizeBypassUrl(bypassUrl)
        };

        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            var existingIndex = store.Mocks.FindIndex(item =>
                string.Equals(item.Method, entry.Method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, entry.Path, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection));

            if (existingIndex >= 0)
            {
                store.Mocks[existingIndex] = entry;
            }
            else
            {
                store.Mocks.Add(entry);
            }

            WriteStoreLocked(store);
        }

        return Task.CompletedTask;
    }

    public Task<MockEntry?> GetAsync(string? collection, string method, string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionReference(collection);
        var normalizedPath = MockStorePersistence.NormalizePath(path);
        MockEntry? entry;

        lock (MockStorePersistence.FileLock)
        {
            entry = ReadStoreLocked().Mocks.FirstOrDefault(item =>
                string.Equals(item.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection));
        }

        return Task.FromResult(entry);
    }

    public Task<IReadOnlyList<MockEntry>> ListAsync(string? collection = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionReference(collection);
        IReadOnlyList<MockEntry> entries;

        lock (MockStorePersistence.FileLock)
        {
            var mocks = ReadStoreLocked().Mocks;
            entries = string.IsNullOrWhiteSpace(normalizedCollection)
                ? mocks
                : mocks.Where(item => MockStorePersistence.MatchesCollection(item, normalizedCollection)).ToList();
        }

        return Task.FromResult(entries);
    }

    public Task RemoveAsync(string? collection, string method, string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionReference(collection);
        var normalizedPath = MockStorePersistence.NormalizePath(path);

        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            store.Mocks.RemoveAll(item =>
                string.Equals(item.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection));
            WriteStoreLocked(store);
        }

        return Task.CompletedTask;
    }

    public Task<bool> UpdateBypassAsync(
        string? collection,
        string method,
        string path,
        bool bypassEnabled,
        string? bypassUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionReference(collection);
        var normalizedPath = MockStorePersistence.NormalizePath(path);
        var updated = false;

        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            var entry = store.Mocks.FirstOrDefault(item =>
                string.Equals(item.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection));

            if (entry != null)
            {
                entry.BypassEnabled = bypassEnabled;
                entry.BypassUrl = MockStorePersistence.NormalizeBypassUrl(bypassUrl);
                updated = true;
                WriteStoreLocked(store);
            }
        }

        return Task.FromResult(updated);
    }

    public Task<bool> UpdateEnabledAsync(
        string collection,
        string method,
        string path,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionId(collection);
        var normalizedPath = MockStorePersistence.NormalizePath(path);
        var updated = false;

        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            var entry = store.Mocks.FirstOrDefault(item =>
                string.Equals(item.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection));

            if (entry != null && !string.IsNullOrWhiteSpace(entry.Collection))
            {
                entry.Enabled = enabled;
                updated = true;
                WriteStoreLocked(store);
            }
        }

        return Task.FromResult(updated);
    }

    private string GetFilePath() => MockStorePersistence.GetFilePath(_configuration, _hostEnvironment);

    private MockStore ReadStoreLocked() => MockStorePersistence.ReadStore(GetFilePath(), _logger);

    private void WriteStoreLocked(MockStore store) => MockStorePersistence.WriteStore(GetFilePath(), store);

    private static JToken? ToResponseBodyToken(object? responseBody)
    {
        if (responseBody == null)
        {
            return null;
        }

        if (responseBody is JToken token)
        {
            return token;
        }

        if (responseBody is string text)
        {
            return new JValue(text);
        }

        return JToken.FromObject(responseBody);
    }
}
