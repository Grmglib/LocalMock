using LocalMock.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalMock.Services;

internal static class MockStorePersistence
{
    internal static readonly object FileLock = new();
    private static readonly Regex CollectionIdPattern = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    internal static readonly HashSet<string> ReservedCollectionIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "collections",
        "bypass"
    };

    internal static string GetFilePath(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        var path = configuration["Mock:FilePath"];
        if (!string.IsNullOrWhiteSpace(path))
        {
            return Path.IsPathRooted(path) ? path : Path.Combine(hostEnvironment.ContentRootPath, path);
        }

        return Path.Combine(hostEnvironment.ContentRootPath, "mocks.json");
    }

    internal static MockStore ReadStore(string filePath, ILogger? logger = null)
    {
        if (!File.Exists(filePath))
        {
            return new MockStore();
        }

        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var token = JToken.Parse(json);

            if (token is JArray array)
            {
                var mocksSemColecao = array.ToObject<List<MockEntry>>() ?? new List<MockEntry>();
                var normalizedMocks = NormalizeMocks(mocksSemColecao);
                ApplyDefaultEnabledForCollectionMocks(normalizedMocks, array);
                return new MockStore
                {
                    Collections = new List<MockCollection>(),
                    Mocks = normalizedMocks
                };
            }

            var store = token.ToObject<MockStore>() ?? new MockStore();
            store.Collections = NormalizeCollections(store.Collections);
            store.Mocks = NormalizeMocks(store.Mocks);
            ApplyDefaultEnabledForCollectionMocks(store.Mocks, token["Mocks"] as JArray);
            return store;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Erro ao ler ou deserializar arquivo de mocks em {FilePath}.", filePath);
            return new MockStore();
        }
    }

    internal static void WriteStore(string filePath, MockStore store)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonConvert.SerializeObject(store, Formatting.Indented);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    internal static List<MockCollection> NormalizeCollections(IEnumerable<MockCollection> collections)
    {
        return collections
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Select(item => new MockCollection
            {
                Id = NormalizeCollectionId(item.Id),
                BypassUrl = NormalizeBypassUrl(item.BypassUrl) ?? string.Empty
            })
            .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    internal static List<MockEntry> NormalizeMocks(IEnumerable<MockEntry> mocks)
    {
        return mocks
            .Where(item => !string.IsNullOrWhiteSpace(item.Method) && !string.IsNullOrWhiteSpace(item.Path))
            .Select(item => new MockEntry
            {
                Collection = NormalizeCollectionReference(item.Collection),
                Method = item.Method.ToUpperInvariant(),
                Path = NormalizePath(item.Path),
                StatusCode = item.StatusCode,
                ResponseDelayMs = NormalizeResponseDelayMs(item.ResponseDelayMs),
                ResponseContentType = NormalizeResponseContentType(item.ResponseContentType),
                ResponseBody = item.ResponseBody,
                Enabled = item.Enabled,
                BypassEnabled = item.BypassEnabled,
                BypassUrl = NormalizeBypassUrl(item.BypassUrl)
            })
            .ToList();
    }

    internal static void ApplyDefaultEnabledForCollectionMocks(List<MockEntry> mocks, JArray? rawMocks)
    {
        if (rawMocks == null)
        {
            return;
        }

        for (var index = 0; index < mocks.Count && index < rawMocks.Count; index++)
        {
            var mock = mocks[index];
            if (string.IsNullOrWhiteSpace(mock.Collection))
            {
                continue;
            }

            if (rawMocks[index] is JObject mockObject && mockObject["Enabled"] == null)
            {
                mock.Enabled = true;
            }
        }
    }

    internal static string NormalizeCollectionId(string id)
    {
        return id.Trim();
    }

    internal static string? NormalizeCollectionReference(string? collection)
    {
        var normalized = collection?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : NormalizeCollectionId(normalized);
    }

    internal static bool IsValidCollectionId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) &&
            CollectionIdPattern.IsMatch(id) &&
            !ReservedCollectionIds.Contains(id);
    }

    internal static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized;
        }

        var queryIndex = normalized.IndexOf('?');
        if (queryIndex >= 0)
        {
            normalized = normalized[..queryIndex];
        }

        return normalized;
    }

    internal static string? NormalizeBypassUrl(string? bypassUrl)
    {
        var normalized = bypassUrl?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    internal static string NormalizeResponseContentType(string? responseContentType)
    {
        var normalized = responseContentType?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "application/json" : normalized;
    }

    internal static int NormalizeResponseDelayMs(int responseDelayMs)
    {
        return Math.Max(0, responseDelayMs);
    }

    internal static bool MatchesCollection(MockEntry entry, string? collection)
    {
        var entryCollection = NormalizeCollectionReference(entry.Collection);
        var targetCollection = NormalizeCollectionReference(collection);
        return string.Equals(entryCollection, targetCollection, StringComparison.OrdinalIgnoreCase);
    }
}
