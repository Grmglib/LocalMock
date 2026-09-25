namespace LocalMock.Models;

/// <summary>Request to update a collection bypass URL.</summary>
public class UpdateCollectionRequest
{
    /// <summary>Absolute HTTP/HTTPS base URL for bypass.</summary>
    public string BypassUrl { get; set; } = string.Empty;
}
