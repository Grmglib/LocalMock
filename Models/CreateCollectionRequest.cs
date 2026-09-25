namespace LocalMock.Models;

/// <summary>Request to create a mock collection.</summary>
public class CreateCollectionRequest
{
    /// <summary>Collection identifier (slug, e.g. partner).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Absolute HTTP/HTTPS base URL for bypass.</summary>
    public string BypassUrl { get; set; } = string.Empty;
}
