using LocalMock.Models;

namespace LocalMock.Services;

public interface IMockService
{
    Task AddAsync(
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
        CancellationToken cancellationToken = default);

    Task<MockEntry?> GetAsync(string? collection, string method, string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MockEntry>> ListAsync(string? collection = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string? collection, string method, string path, CancellationToken cancellationToken = default);
    Task<bool> UpdateBypassAsync(
        string? collection,
        string method,
        string path,
        bool bypassEnabled,
        string? bypassUrl,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateEnabledAsync(
        string collection,
        string method,
        string path,
        bool enabled,
        CancellationToken cancellationToken = default);
}
