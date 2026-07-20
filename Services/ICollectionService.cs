using LocalMock.Models;

namespace LocalMock.Services;

public interface ICollectionService
{
    Task<IReadOnlyList<MockCollection>> ListAsync(CancellationToken cancellationToken = default);
    Task<MockCollection?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(string id, string bypassUrl, CancellationToken cancellationToken = default);
    Task<bool> UpdateBypassAsync(string id, string bypassUrl, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> RemoveAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> HasMocksAsync(string id, CancellationToken cancellationToken = default);
}
