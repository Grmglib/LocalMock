using Microsoft.AspNetCore.Http;

namespace LocalMock.Services;

public interface IBypassProxyService
{
    Task<bool> ForwardAsync(
        HttpContext context,
        string bypassUrl,
        string? pathPrefixToStrip = null,
        CancellationToken cancellationToken = default);
}
