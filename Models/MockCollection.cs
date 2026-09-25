namespace LocalMock.Models;

/// <summary>Mock collection with a bypass URL for unconfigured endpoints.</summary>
public class MockCollection
{
    /// <summary>Collection identifier (slug, e.g. partner).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Absolute HTTP/HTTPS base URL used for forwarding when no mock matches.</summary>
    public string BypassUrl { get; set; } = string.Empty;
}
