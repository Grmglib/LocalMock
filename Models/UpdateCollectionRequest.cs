namespace LocalMock.Models;

/// <summary>Request para atualização da URL de bypass de uma coleção.</summary>
public class UpdateCollectionRequest
{
    /// <summary>URL base absoluta HTTP/HTTPS para bypass.</summary>
    public string BypassUrl { get; set; } = string.Empty;
}
