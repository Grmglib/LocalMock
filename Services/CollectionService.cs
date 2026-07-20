using LocalMock.Models;

namespace LocalMock.Services;

public class CollectionService : ICollectionService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<CollectionService> logger)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public Task<IReadOnlyList<MockCollection>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            return Task.FromResult<IReadOnlyList<MockCollection>>(store.Collections);
        }
    }

    public Task<MockCollection?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);

        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            var collection = store.Collections.FirstOrDefault(item =>
                string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(collection);
        }
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);

        lock (MockStorePersistence.FileLock)
        {
            var exists = ReadStoreLocked().Collections.Any(item =>
                string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
    }

    public Task<bool> CreateAsync(string id, string bypassUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);
        var normalizedBypassUrl = MockStorePersistence.NormalizeBypassUrl(bypassUrl) ?? string.Empty;

        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            if (store.Collections.Any(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            store.Collections.Add(new MockCollection
            {
                Id = normalizedId,
                BypassUrl = normalizedBypassUrl
            });

            WriteStoreLocked(store);
        }

        return Task.FromResult(true);
    }

    public Task<bool> UpdateBypassAsync(string id, string bypassUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);
        var normalizedBypassUrl = MockStorePersistence.NormalizeBypassUrl(bypassUrl) ?? string.Empty;
        var updated = false;

        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            var collection = store.Collections.FirstOrDefault(item =>
                string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));

            if (collection != null)
            {
                collection.BypassUrl = normalizedBypassUrl;
                updated = true;
                WriteStoreLocked(store);
            }
        }

        return Task.FromResult(updated);
    }

    public Task<(bool Success, string? Error)> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);

        lock (MockStorePersistence.FileLock)
        {
            var store = ReadStoreLocked();
            var removed = store.Collections.RemoveAll(item =>
                string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)) > 0;

            if (!removed)
            {
                return Task.FromResult<(bool, string?)>((false, "Coleção não encontrada."));
            }

            store.Mocks.RemoveAll(item => MockStorePersistence.MatchesCollection(item, normalizedId));
            WriteStoreLocked(store);

            return Task.FromResult<(bool, string?)>((true, null));
        }
    }

    public Task<bool> HasMocksAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);

        lock (MockStorePersistence.FileLock)
        {
            var hasMocks = ReadStoreLocked().Mocks.Any(item => MockStorePersistence.MatchesCollection(item, normalizedId));
            return Task.FromResult(hasMocks);
        }
    }

    private string GetFilePath() => MockStorePersistence.GetFilePath(_configuration, _hostEnvironment);

    private MockStore ReadStoreLocked() => MockStorePersistence.ReadStore(GetFilePath(), _logger);

    private void WriteStoreLocked(MockStore store) => MockStorePersistence.WriteStore(GetFilePath(), store);
}
