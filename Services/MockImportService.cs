using LocalMock.Models;
using Newtonsoft.Json.Linq;

namespace LocalMock.Services;

public sealed class MockImportService
{
    private readonly IMockStoreRepository _repository;

    public MockImportService(IMockStoreRepository repository) => _repository = repository;

    public (ImportBatchResult? Result, string? Error) Import(ImportBatchRequest request)
    {
        return _repository.ExecuteLocked<(ImportBatchResult? Result, string? Error)>((store, save) =>
        {
            var collections = new List<MockCollection>();
            foreach (var item in request.Collections)
            {
                var id = MockStorePersistence.NormalizeCollectionId(item.Id ?? string.Empty);
                if (!MockStorePersistence.IsValidCollectionId(id) || !ValidHttpUrl(item.BypassUrl))
                    return (null, $"Collection '{item.Id}' has an invalid ID or bypass URL.");
                if (collections.Any(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)))
                    return (null, $"Collection '{id}' appears more than once in the import.");
                collections.Add(new MockCollection { Id = id, BypassUrl = MockStorePersistence.NormalizeBypassUrl(item.BypassUrl)! });
            }

            var mocks = new List<(MockEntry Entry, bool Overwrite)>();
            foreach (var item in request.Mocks)
            {
                var source = item.Mock;
                var collection = MockStorePersistence.NormalizeCollectionReference(source.Collection);
                var method = (source.Method ?? string.Empty).Trim().ToUpperInvariant();
                var path = MockStorePersistence.NormalizePath(source.Path ?? string.Empty);
                if (!new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }.Contains(method) ||
                    path == "/" || source.StatusCode is < 100 or > 599 || source.ResponseDelayMs < 0)
                    return (null, $"Mock '{method} {path}' has invalid fields.");
                if (!string.IsNullOrWhiteSpace(collection) &&
                    !store.Collections.Any(x => string.Equals(x.Id, collection, StringComparison.OrdinalIgnoreCase)) &&
                    !collections.Any(x => string.Equals(x.Id, collection, StringComparison.OrdinalIgnoreCase)))
                    return (null, $"Mock '{method} {path}' references collection '{collection}', which is not selected or already present.");

                var entry = new MockEntry
                {
                    Collection = collection,
                    Method = method,
                    Path = path,
                    StatusCode = source.StatusCode,
                    ResponseDelayMs = source.ResponseDelayMs,
                    ResponseContentType = MockStorePersistence.NormalizeResponseContentType(source.ResponseContentType),
                    ResponseBody = source.ResponseBody?.DeepClone(),
                    Enabled = string.IsNullOrWhiteSpace(collection) || source.Enabled,
                    BypassEnabled = source.BypassEnabled,
                    BypassUrl = MockStorePersistence.NormalizeBypassUrl(source.BypassUrl)
                };
                if (entry.BypassEnabled && !ValidHttpUrl(entry.BypassUrl))
                    return (null, $"Mock '{method} {path}' has an invalid bypass URL.");
                if (mocks.Any(x => SameMock(x.Entry, entry)))
                    return (null, $"Mock '{method} {path}' appears more than once in the import.");
                mocks.Add((entry, item.Overwrite));
            }

            foreach (var (entry, overwrite) in mocks)
            {
                var existing = store.Mocks.FirstOrDefault(x => SameMock(x, entry));
                if (existing != null && !overwrite)
                    return (null, $"Mock '{entry.Method} {entry.Path}' already exists. Select replace to overwrite it.");
            }

            var result = new ImportBatchResult();
            foreach (var collection in collections)
            {
                var existing = store.Collections.FirstOrDefault(x => string.Equals(x.Id, collection.Id, StringComparison.OrdinalIgnoreCase));
                var requested = request.Collections.First(x => string.Equals(MockStorePersistence.NormalizeCollectionId(x.Id), collection.Id, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    store.Collections.Add(collection);
                    result.CollectionsAdded++;
                }
                else if (!string.Equals(existing.BypassUrl, collection.BypassUrl, StringComparison.OrdinalIgnoreCase))
                {
                    if (!requested.Overwrite)
                        return (null, $"Collection '{collection.Id}' already exists with a different bypass URL. Select replace to overwrite it.");
                    existing.BypassUrl = collection.BypassUrl;
                    result.CollectionsUpdated++;
                }
            }

            foreach (var (entry, _) in mocks)
            {
                var index = store.Mocks.FindIndex(x => SameMock(x, entry));
                if (index < 0)
                {
                    store.Mocks.Add(entry);
                    result.MocksAdded++;
                }
                else
                {
                    store.Mocks[index] = entry;
                    result.MocksUpdated++;
                }
            }

            if (result.CollectionsAdded + result.CollectionsUpdated + result.MocksAdded + result.MocksUpdated > 0)
                save();
            return (result, null);
        });
    }

    private static bool SameMock(MockEntry left, MockEntry right) =>
        string.Equals(left.Method, right.Method, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Path, right.Path, StringComparison.Ordinal) &&
        MockStorePersistence.MatchesCollection(left, right.Collection);

    private static bool ValidHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
