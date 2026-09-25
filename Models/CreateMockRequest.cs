using Newtonsoft.Json.Linq;

namespace LocalMock.Models;

/// <summary>Request to create an endpoint mock.</summary>
public class CreateMockRequest
{
    /// <summary>Collection identifier. Optional for standalone mocks.</summary>
    public string? Collection { get; set; }

    /// <summary>HTTP method (GET, POST, PUT, DELETE, PATCH).</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Endpoint path (e.g. /customers).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>HTTP status code of the response.</summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>Delay before returning the mocked response, in milliseconds.</summary>
    public int ResponseDelayMs { get; set; }

    /// <summary>Content-Type of the mocked response.</summary>
    public string? ResponseContentType { get; set; }

    /// <summary>Response body (JSON object, form-urlencoded string, plain text, etc.).</summary>
    public object? ResponseBody { get; set; }

    /// <summary>Whether the mock is active (collections). Default: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether the call should be forwarded to an external URL.</summary>
    public bool BypassEnabled { get; set; }

    /// <summary>Absolute URL that receives the call when bypass is enabled.</summary>
    public string? BypassUrl { get; set; }
}
