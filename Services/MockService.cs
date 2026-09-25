using LocalMock.Models;
using Newtonsoft.Json.Linq;

namespace LocalMock.Services;

public class MockService : IMockService
{
    private readonly IMockStoreRepository _storeRepository;

    public MockService(IMockStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
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

        _storeRepository.ExecuteLocked((store, save) =>
        {
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

            save();
        });

        return Task.CompletedTask;
    }

    public Task<MockEntry?> GetAsync(string? collection, string method, string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionReference(collection);
        var normalizedPath = MockStorePersistence.NormalizePath(path);

        var entry = _storeRepository.ExecuteLocked((store, _) =>
            store.Mocks.FirstOrDefault(item =>
                string.Equals(item.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection)));

        return Task.FromResult(entry);
    }

    public Task<IReadOnlyList<MockEntry>> ListAsync(string? collection = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionReference(collection);

        var entries = _storeRepository.ExecuteLocked((store, _) =>
        {
            IReadOnlyList<MockEntry> list = string.IsNullOrWhiteSpace(normalizedCollection)
                ? store.Mocks.ToList()
                : store.Mocks.Where(item => MockStorePersistence.MatchesCollection(item, normalizedCollection)).ToList();
            return list;
        });

        return Task.FromResult(entries);
    }

    public Task RemoveAsync(string? collection, string method, string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedCollection = MockStorePersistence.NormalizeCollectionReference(collection);
        var normalizedPath = MockStorePersistence.NormalizePath(path);

        _storeRepository.ExecuteLocked((store, save) =>
        {
            store.Mocks.RemoveAll(item =>
                string.Equals(item.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection));
            save();
        });

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

        var updated = _storeRepository.ExecuteLocked((store, save) =>
        {
            var entry = store.Mocks.FirstOrDefault(item =>
                string.Equals(item.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection));

            if (entry == null)
            {
                return false;
            }

            entry.BypassEnabled = bypassEnabled;
            entry.BypassUrl = MockStorePersistence.NormalizeBypassUrl(bypassUrl);
            save();
            return true;
        });

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

        var updated = _storeRepository.ExecuteLocked((store, save) =>
        {
            var entry = store.Mocks.FirstOrDefault(item =>
                string.Equals(item.Method, method, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal) &&
                MockStorePersistence.MatchesCollection(item, normalizedCollection));

            if (entry == null || string.IsNullOrWhiteSpace(entry.Collection))
            {
                return false;
            }

            entry.Enabled = enabled;
            save();
            return true;
        });

        return Task.FromResult(updated);
    }

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
