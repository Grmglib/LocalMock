namespace LocalMock.Models;

/// <summary>Request para criação de uma coleção de mocks.</summary>
public class CreateCollectionRequest
{
    /// <summary>Identificador da coleção (slug, ex.: parceiro).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>URL base absoluta HTTP/HTTPS para bypass.</summary>
    public string BypassUrl { get; set; } = string.Empty;
}
