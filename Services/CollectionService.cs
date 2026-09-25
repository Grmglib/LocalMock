using LocalMock.Models;

namespace LocalMock.Services;

public class CollectionService : ICollectionService
{
    private readonly IMockStoreRepository _storeRepository;

    public CollectionService(IMockStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    public Task<IReadOnlyList<MockCollection>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var list = _storeRepository.ExecuteLocked((store, _) =>
            (IReadOnlyList<MockCollection>)store.Collections.ToList());
        return Task.FromResult(list);
    }

    public Task<MockCollection?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);

        var collection = _storeRepository.ExecuteLocked((store, _) =>
            store.Collections.FirstOrDefault(item =>
                string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(collection);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);

        var exists = _storeRepository.ExecuteLocked((store, _) =>
            store.Collections.Any(item =>
                string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(exists);
    }

    public Task<bool> CreateAsync(string id, string bypassUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);
        var normalizedBypassUrl = MockStorePersistence.NormalizeBypassUrl(bypassUrl) ?? string.Empty;

        var created = _storeRepository.ExecuteLocked((store, save) =>
        {
            if (store.Collections.Any(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            store.Collections.Add(new MockCollection
            {
                Id = normalizedId,
                BypassUrl = normalizedBypassUrl
            });

            save();
            return true;
        });

        return Task.FromResult(created);
    }

    public Task<bool> UpdateBypassAsync(string id, string bypassUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);
        var normalizedBypassUrl = MockStorePersistence.NormalizeBypassUrl(bypassUrl) ?? string.Empty;

        var updated = _storeRepository.ExecuteLocked((store, save) =>
        {
            var collection = store.Collections.FirstOrDefault(item =>
                string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));

            if (collection == null)
            {
                return false;
            }

            collection.BypassUrl = normalizedBypassUrl;
            save();
            return true;
        });

        return Task.FromResult(updated);
    }

    public Task<(bool Success, string? Error)> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);

        var result = _storeRepository.ExecuteLocked((store, save) =>
        {
            var removed = store.Collections.RemoveAll(item =>
                string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)) > 0;

            if (!removed)
            {
                return (false, "Collection not found.");
            }

            store.Mocks.RemoveAll(item => MockStorePersistence.MatchesCollection(item, normalizedId));
            save();
            return (true, (string?)null);
        });

        return Task.FromResult(result);
    }

    public Task<bool> HasMocksAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = MockStorePersistence.NormalizeCollectionId(id);

        var hasMocks = _storeRepository.ExecuteLocked((store, _) =>
            store.Mocks.Any(item => MockStorePersistence.MatchesCollection(item, normalizedId)));

        return Task.FromResult(hasMocks);
    }
}
