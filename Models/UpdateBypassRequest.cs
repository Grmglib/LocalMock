namespace LocalMock.Models;

/// <summary>Request to enable or disable bypass for a saved mock.</summary>
public class UpdateBypassRequest
{
    /// <summary>Collection identifier. Optional for standalone mocks.</summary>
    public string? Collection { get; set; }

    /// <summary>HTTP method of the mock.</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Path of the mocked endpoint.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Whether the call should be forwarded to the bypass URL.</summary>
    public bool BypassEnabled { get; set; }

    /// <summary>Absolute URL that receives the call when bypass is enabled.</summary>
    public string? BypassUrl { get; set; }
}
